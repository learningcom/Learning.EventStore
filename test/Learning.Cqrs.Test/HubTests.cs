using System;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.Cqrs.Test
{
    [TestClass]
    public class HubTests
    {
        private IHub _hub;
        private Func<Type, object> _activator;

        [TestInitialize]
        public void SetUp()
        {
            _activator = A.Fake<Func<Type, object>>();
            _hub = new Hub(_activator);
        }

        [TestMethod]
        public void CorrectQueryHandlerUsed()
        {
            var handler = A.Fake<IQueryHandler<TestQuery, string>>();

            const string expectedResult = "result";

            A.CallTo(() => _activator(typeof(IQueryHandler<TestQuery, string>))).Returns(handler);
            A.CallTo(() => handler.Handle(A<TestQuery>.Ignored)).Returns(expectedResult);

            var result = _hub.Query(new TestQuery());

            Assert.AreEqual(expectedResult, result);

            A.CallTo(() => handler.Handle(A<TestQuery>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void CorrectCommandHandlerUsed()
        {
            var handler = A.Fake<ICommandHandler<TestCommand>>();

            A.CallTo(() => _activator(typeof(ICommandHandler<TestCommand>))).Returns(handler);

            var success = _hub.Command(new TestCommand());

            A.Equals(true, success);
            A.CallTo(() => handler.Handle(A<TestCommand>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task CorrectAsyncQueryHandlerUsed()
        {
            var handler = A.Fake<IAsyncQueryHandler<TestQuery, string>>();

            const string expectedResult = "result";

            A.CallTo(() => _activator(typeof(IAsyncQueryHandler<TestQuery, string>))).Returns(handler);
            A.CallTo(() => handler.Handle(A<TestQuery>.Ignored)).Returns(expectedResult);

            var result = await _hub.QueryAsync(new TestQuery());

            Assert.AreEqual(expectedResult, result);

            A.CallTo(() => handler.Handle(A<TestQuery>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task CorrectAsyncCommandHandlerUsed()
        {
            var handler = A.Fake<IAsyncCommandHandler<TestCommand>>();

            A.CallTo(() => _activator(typeof(IAsyncCommandHandler<TestCommand>))).Returns(handler);

            await _hub.CommandAsync(new TestCommand());

            A.CallTo(() => handler.Handle(A<TestCommand>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void ErrorWhenNoHandlerFound()
        {
            A.CallTo(() => _activator(typeof(IQueryHandler<MissingQuery, string>))).Returns(null);
            var exception = Assert.ThrowsException<CqrsException>(() => _hub.Query(new MissingQuery()));
            Assert.AreEqual("Non-Async handler was not found for type MissingQuery", exception.Message);
        }

        [TestMethod]
        public void ErrorWhenNoCommandHandlerFound()
        {
            A.CallTo(() => _activator(typeof(ICommandHandler<MissingCommand>))).Returns(null);
            var exception = Assert.ThrowsException<CqrsException>(() => _hub.Command(new MissingCommand()));
            Assert.AreEqual("Non-Async handler was not found for type MissingCommand", exception.Message);
        }

        [TestMethod]
        public async Task ErrorWhenNoAsyncQueryHandlerFound()
        {
            A.CallTo(() => _activator(typeof(IAsyncQueryHandler<MissingQuery, string>))).Returns(null);
            var exception = await Assert.ThrowsExceptionAsync<CqrsException>(() => _hub.QueryAsync(new MissingQuery()));
            Assert.AreEqual("Async handler was not found for type MissingQuery", exception.Message);
        }

        [TestMethod]
        public async Task ErrorWhenAsyncCommandHandlerFound()
        {
            A.CallTo(() => _activator(typeof(IAsyncCommandHandler<MissingCommand>))).Returns(null);
            var exception = await Assert.ThrowsExceptionAsync<CqrsException>(() => _hub.CommandAsync(new MissingCommand()));
            Assert.AreEqual("Async handler was not found for type MissingCommand", exception.Message);
        }

    }
}
