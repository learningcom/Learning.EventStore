SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

  /**************************************************************************************************
* Name:         SaveEventForAggregate
* Description:  Inserts a new Event for an Aggregate.
* Author:       Tom Mooney
* Date Created: 2018MAR20
*
* History: 2018MAR20 - tmooney - Created procedure
*
**************************************************************************************************/
CREATE procedure [dbo].[SaveEventForAggregate]
(
	@AggregateId [varchar] (255) , 
	@AggregateType [varchar] (255) ,
	@ApplicationName [varchar] (255) ,
	@Version [int] ,
	@TimeStamp [datetimeoffset] (7) ,
    @EventType [varchar] (255) ,
    @EventData [nvarchar] (max)
)
AS
BEGIN
	SET NOCOUNT ON;
	SET XACT_ABORT ON;

    DECLARE @AggregateKey bigint
	SET @AggregateKey = (SELECT AggregateKey 
						FROM dbo.Aggregate 
						WHERE AggregateId = @AggregateId
						AND AggregateType = @AggregateType
						AND ApplicationName = @ApplicationName)
	
	IF @AggregateKey IS NULL
		BEGIN
			INSERT INTO dbo.Aggregate
			(
				[AggregateId], 
				[AggregateType],
				[ApplicationName]
			)
			SELECT @AggregateId ,
				@AggregateType , 
				@ApplicationName
			
			SET @AggregateKey = SCOPE_IDENTITY()
		END

	INSERT INTO dbo.Event
	(
		[AggregateKey],
		[TimeStamp],
		[Version],
		[EventType],
		[EventData]
	)
	VALUES
	(
		@AggregateKey,
		@TimeStamp,
		@Version,
		@EventType,
		@EventData
	)
END;
GO
GRANT EXECUTE ON  [dbo].[SaveEventForAggregate] TO [lc_ReaderWriter]
GO
