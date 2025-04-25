using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct CheckInPolygonJob : IJobParallelFor
{
    [ReadOnly] public float2 point;
    [ReadOnly] public NativeArray<float2> pAs;
    [ReadOnly] public NativeArray<float2> pBs;
    public NativeArray<bool> isPointInPolygon;
    public void Execute(int index)
    {
        float2 a = pAs[index];
        float2 b = pBs[index];

        if ((a.y > point.y) != (b.y > point.y))
        {
            float slope = (point.y - a.y) / (b.y - a.y);
            if (point.x < (a.x + (b.x - a.x) * slope))
                isPointInPolygon[0] = !isPointInPolygon[0];
        }
    }
}

public struct MinDistanceInfo
{
    public int pointIndex;
    public float tag;
    public float minDistance;//点到线段的距离
    public float2 nearestPoint;//点到直线上最近的点
    public float2 projectedLineDirection;//被投影线段的方向
}

[BurstCompile]
public struct MinDistanceInfoJobParallel : IJobParallelFor
{
    [ReadOnly] public float2 point;
    [ReadOnly] public NativeArray<float2> pAs;
    [ReadOnly] public NativeArray<float2> pBs;
    [WriteOnly] public NativeArray<MinDistanceInfo> minDistanceInfo;

    public void Execute(int index)
    {
        float2 a = pAs[index];
        float2 b = pBs[index];

        float2 ab = b - a;
        float2 ap = point - a;

        float sqrLength = math.dot(ab, ab);
        float projection = math.dot(ap, ab);

        float t = math.clamp(projection / sqrLength, 0f, 1f);
        float2 nearest = a + t * ab;

        minDistanceInfo[index] = new MinDistanceInfo
        {
            pointIndex = index,
            tag = a.x * a.y,
            minDistance = math.distance(point, nearest),
            nearestPoint = nearest,
            projectedLineDirection = ab
        };
    }
}