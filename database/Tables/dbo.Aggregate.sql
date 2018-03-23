CREATE TABLE [dbo].[Aggregate]
(
[AggregateKey] [bigint] NOT NULL IDENTITY(1, 1),
[AggregateId] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[AggregateType] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[ApplicationName] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[auditCreateDate] [datetime2] (4) NOT NULL CONSTRAINT [defAggregate_auditCreateDate] DEFAULT (sysdatetime()),
[auditCreateUser] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [defAggregate_auditCreateUser] DEFAULT (suser_sname())
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Aggregate] ADD CONSTRAINT [pkAggregate] PRIMARY KEY CLUSTERED ([AggregateKey]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Aggregate] ADD CONSTRAINT [uqcAggregateId_ApplicationName_AggregateType] UNIQUE ([AggregateId], [ApplicationName], [AggregateType])
GO
