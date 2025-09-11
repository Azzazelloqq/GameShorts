namespace Code.Core.BaseDMDisposable.Scripts
{
    public class BaseClass
    {
        private DDebug _baseLogger;
        protected DDebug log
            => _baseLogger ??= new DDebug(GetType().Name);
    }
}