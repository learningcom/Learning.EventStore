CREATE TABLE [dbo].[Event]
(
[EventId] [bigint] NOT NULL IDENTITY(1, 1),
[AggregateKey] [bigint] NOT NULL,
[TimeStamp] [datetimeoffset] (7) NOT NULL,
[Version] [int] NOT NULL,
[EventType] [varchar] (255) NOT NULL,
[EventData] [nvarchar] (max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[auditCreateDate] [datetime2] (4) NOT NULL CONSTRAINT [defEvent_auditCreateDate] DEFAULT (sysdatetime()),
[auditCreateUser] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [defEvent_auditCreateUser] DEFAULT (suser_sname()),
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[Event] ADD CONSTRAINT [pkEvent] PRIMARY KEY CLUSTERED ([EventId]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Event] ADD CONSTRAINT [fkEvent_Aggregate] FOREIGN KEY ([AggregateKey]) REFERENCES [dbo].[Aggregate] ([AggregateKey])
GO
CREATE UNIQUE NONCLUSTERED INDEX [idxEvent_AggregateKey_Version] ON [dbo].[Event] ([AggregateKey], [Version]) INCLUDE ([EventData]) ON [PRIMARY]
GO