namespace Application.Posts.Queries.GetPostClusters;

public class ClusterDto
{
    public int ClusterId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Count { get; set; }
}
