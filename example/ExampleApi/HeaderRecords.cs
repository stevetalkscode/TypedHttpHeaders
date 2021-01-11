using System.Collections.Generic;

namespace ExampleApi
{
    public abstract record HeaderBase<T>
    {
        protected HeaderBase()
        {
            HasValue = false;
        }

        protected HeaderBase(T value)
        {
            Value = value;
            HasValue = true;
        }

        public T Value { get; }

        public bool HasValue { get; }


    }
    public record ExternalCorrelation : HeaderBase<string>
    {
        public ExternalCorrelation() 
        {
        }

        public ExternalCorrelation(string value) : base(value)
        {
        }
    }

    public record InternalCorrelation : HeaderBase<string[]>
    {
        public InternalCorrelation() 
        {
        }

        public InternalCorrelation(string[] value) : base(value)
        {
        }
    }

    public interface IAllCorrelation
    {
        IReadOnlyCollection<string> Value { get; }
        bool HasValue { get; }
    }

    public record AllCorrelation : HeaderBase<IReadOnlyCollection<string>>, IAllCorrelation
    {
        public AllCorrelation() 
        {
        }

        public AllCorrelation(IReadOnlyCollection<string> value) : base(value)
        {
        }
    }
    
    
}
