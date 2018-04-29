# Learning.EventStore #

Learning.EventStore is a framework for CQRS, Eventsourcing, and messaging based on [CQRSLite](https://github.com/gautema/cqrslite) that uses Redis pub/sub for messaging and offers event persistence in Redis, SQL Server, or PostgreSQL. It is written in C# and targets .NET 4.5.2, .NET 4.6, and .NET Core. 

# Installation #

There are three separate libraries included in this repository. All three are available as Nuget packages and can be installed from there.

* [Learning.Cqrs](https://www.nuget.org/packages/Learning.Cqrs/)
* [Learning.MessageQueue](https://www.nuget.org/packages/Learning.MessageQueue/)
* [Learning.EventStore](https://www.nuget.org/packages/Learning.EventStore/)

# Getting Started #

There is a sample application included in this repository that shows basic set up and use of the framework. There is also a full suite of unit tests that documents the functionality. Additional documentation is currently a work in progress and will be coming soon.