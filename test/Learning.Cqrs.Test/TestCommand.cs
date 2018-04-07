namespace Learning.Cqrs.Test
{
    public class TestCommand : ICommand
    {
        public class Handler : ICommandHandler<TestCommand>
        {
            public bool Handle(TestCommand command)
            {
                return true;
            }
        }
    }
}
