using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.Tools;
using InGameLogger;

namespace Code.Core.GameSwiper
{
    /// <summary>
    /// Manages a queue of swipe requests to support rapid consecutive swipes
    /// Similar to TikTok/YouTube Shorts behavior
    /// </summary>
    public class SwipeQueueManager : IDisposable
    {
        private readonly Queue<SwipeRequest> _swipeQueue = new Queue<SwipeRequest>();
        private readonly GameSwiperService _swiperService;
        private readonly IInGameLogger _logger;
        private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cancellationTokenSource;
        private Task _processingTask;
        private bool _isProcessing;
        private readonly int _maxQueueSize;
        
        public enum SwipeType
        {
            Next,
            Previous
        }
        
        private class SwipeRequest
        {
            public SwipeType Type { get; set; }
            public DateTime RequestTime { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }
        
        /// <summary>
        /// Event fired when queue size changes
        /// </summary>
        public event Action<int> OnQueueSizeChanged;
        
        /// <summary>
        /// Event fired when a swipe is completed
        /// </summary>
        public event Action<SwipeType, bool> OnSwipeCompleted;
        
        public int QueueSize => _swipeQueue.Count;
        public bool IsProcessing => _isProcessing;
        
        public SwipeQueueManager(
            GameSwiperService swiperService, 
            IInGameLogger logger,
            int maxQueueSize = 3)
        {
            _swiperService = swiperService ?? throw new ArgumentNullException(nameof(swiperService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxQueueSize = maxQueueSize;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Start the processing loop
            _processingTask = ProcessQueueAsync(_cancellationTokenSource.Token);
        }
        
        /// <summary>
        /// Enqueues a swipe request
        /// </summary>
        public Task<bool> EnqueueSwipeAsync(SwipeType type, CancellationToken cancellationToken = default)
        {
            // Check if we can accept more swipes
            if (_swipeQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning($"Swipe queue is full (max: {_maxQueueSize}). Ignoring swipe request.");
                return Task.FromResult(false);
            }
            
            // Check if the swipe type is valid
            if (type == SwipeType.Next && !_swiperService.CanSwipeNext)
            {
                _logger.LogWarning("Cannot queue next swipe - no next game available");
                return Task.FromResult(false);
            }
            
            if (type == SwipeType.Previous && !_swiperService.CanSwipePrevious)
            {
                _logger.LogWarning("Cannot queue previous swipe - no previous game available");
                return Task.FromResult(false);
            }
            
            // Create request
            var request = new SwipeRequest
            {
                Type = type,
                RequestTime = DateTime.Now,
                CompletionSource = new TaskCompletionSource<bool>()
            };
            
            // Enqueue
            lock (_swipeQueue)
            {
                _swipeQueue.Enqueue(request);
                _logger.Log($"Enqueued {type} swipe. Queue size: {_swipeQueue.Count}");
                OnQueueSizeChanged?.Invoke(_swipeQueue.Count);
            }
            
            // Register cancellation
            cancellationToken.Register(() =>
            {
                request.CompletionSource.TrySetCanceled(cancellationToken);
            });
            
            return request.CompletionSource.Task;
        }
        
        /// <summary>
        /// Clears all pending swipes from the queue
        /// </summary>
        public void ClearQueue()
        {
            lock (_swipeQueue)
            {
                while (_swipeQueue.Count > 0)
                {
                    var request = _swipeQueue.Dequeue();
                    request.CompletionSource.TrySetResult(false);
                }
                
                _logger.Log("Cleared swipe queue");
                OnQueueSizeChanged?.Invoke(0);
            }
        }
        
        /// <summary>
        /// Processes the queue continuously
        /// </summary>
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SwipeRequest request = null;
                
                // Get next request from queue
                lock (_swipeQueue)
                {
                    if (_swipeQueue.Count > 0)
                    {
                        request = _swipeQueue.Dequeue();
                        OnQueueSizeChanged?.Invoke(_swipeQueue.Count);
                    }
                }
                
                if (request != null)
                {
                    await _processingSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        _isProcessing = true;
                        
                        // Check if request is too old (optional timeout)
                        if (DateTime.Now - request.RequestTime > TimeSpan.FromSeconds(10))
                        {
                            _logger.LogWarning($"Swipe request timed out after 10 seconds");
                            request.CompletionSource.TrySetResult(false);
                            continue;
                        }
                        
                        // Process the swipe
                        bool result = false;
                        try
                        {
                            _logger.Log($"Processing {request.Type} swipe from queue");
                            
                            // Wait for any current transition to complete
                            while (_swiperService.IsTransitioning && !cancellationToken.IsCancellationRequested)
                            {
                                await Task.Delay(50, cancellationToken);
                            }
                            
                            // Execute the swipe
                            switch (request.Type)
                            {
                                case SwipeType.Next:
                                    result = await _swiperService.SwipeToNextGameAsync(cancellationToken);
                                    break;
                                case SwipeType.Previous:
                                    result = await _swiperService.SwipeToPreviousGameAsync(cancellationToken);
                                    break;
                            }
                            
                            request.CompletionSource.TrySetResult(result);
                            OnSwipeCompleted?.Invoke(request.Type, result);
                            
                            _logger.Log($"Completed {request.Type} swipe. Success: {result}. Remaining queue: {_swipeQueue.Count}");
                        }
                        catch (OperationCanceledException)
                        {
                            request.CompletionSource.TrySetCanceled(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error processing swipe: {ex.Message}");
                            request.CompletionSource.TrySetException(ex);
                        }
                    }
                    finally
                    {
                        _isProcessing = false;
                        _processingSemaphore.Release();
                    }
                }
                else
                {
                    // No requests, wait a bit
                    await Task.Delay(50, cancellationToken);
                }
            }
        }
        
        /// <summary>
        /// Gets information about the current queue
        /// </summary>
        public string GetQueueInfo()
        {
            lock (_swipeQueue)
            {
                if (_swipeQueue.Count == 0)
                    return "Queue is empty";
                
                var types = new List<string>();
                foreach (var request in _swipeQueue)
                {
                    types.Add(request.Type.ToString());
                }
                
                return $"Queue ({_swipeQueue.Count}): {string.Join(" -> ", types)}";
            }
        }
        
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            
            // Clear remaining requests
            ClearQueue();
            
            // Wait for processing to complete
            try
            {
                _processingTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
            
            _cancellationTokenSource?.Dispose();
            _processingSemaphore?.Dispose();
        }
    }
}
