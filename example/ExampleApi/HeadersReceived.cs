namespace ExampleApi
{
    // ReSharper disable once UnusedMember.Global
    public record HeadersReceived(ExternalCorrelation External, InternalCorrelation Internal, IAllCorrelation All);

}
