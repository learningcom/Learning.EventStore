namespace Learning.Cqrs.Test
{
    public class TestQuery : IQuery<string>
    {
        public class Handler : IQueryHandler<TestQuery, string>
        {
            public string Handle(TestQuery query)
            {
                return "Hello world!";
            }
        }
    }
}
