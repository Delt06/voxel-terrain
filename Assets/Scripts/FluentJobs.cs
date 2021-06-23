using Unity.Collections;
using Unity.Jobs;

public static class FluentJobs
{
    public static JobHandle Then<T>(in this JobHandle jobHandle, T job) where T : struct, IJob =>
        job.Schedule(jobHandle);

    public static JobHandle ThenDispose<T>(in this JobHandle jobHandle, T disposable)
        where T : struct, INativeDisposable =>
        disposable.Dispose(jobHandle);

    public static JobHandle ThenDispose<T>(in this JobHandle jobHandle, NativeArray<T> array)
        where T : struct =>
        array.Dispose(jobHandle);
}