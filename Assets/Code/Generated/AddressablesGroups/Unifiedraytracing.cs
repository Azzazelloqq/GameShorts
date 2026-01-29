using System;

namespace Code.Generated.Addressables
{
    public class Unifiedraytracing
    {
        public readonly PackagesSubGroup Packages = new PackagesSubGroup();

        public class PackagesSubGroup
        {
            public class ComUnityRenderPipelinesCoreSubGroup
            {
                public class RuntimeSubGroup
                {
                    public class UnifiedRayTracingSubGroup
                    {
                        public class ComputeSubGroup
                        {
                            public class RadeonRaysSubGroup
                            {
                                public class KernelsSubGroup
                                {
                                    public string CopyPositionsCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/CopyPositions.compute";
                                    public string BuildHlbvhCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/build_hlbvh.compute";
                                    public string BlockScanCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/block_scan.compute";
                                    public string BlockReducePartCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/block_reduce_part.compute";
                                    public string RestructureBvhCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/restructure_bvh.compute";
                                    public string BitHistogramCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/bit_histogram.compute";
                                    public string ScatterCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/scatter.compute";
                                }
                            }
                        }

                        public class CommonSubGroup
                        {
                            public class UtilitiesSubGroup
                            {
                                public string CopyBufferCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common/Utilities/CopyBuffer.compute";
                            }

                            public class GeometryPoolSubGroup
                            {
                                public string GeometryPoolKernelsCompute = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common/GeometryPool/GeometryPoolKernels.compute";
                            }
                        }
                    }
                }
            }
        }
    }
}
