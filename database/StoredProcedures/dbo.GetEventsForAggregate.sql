SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

  /**************************************************************************************************
* Name:         GetEventsForAggregate
* Description:  Gets all event data for an aggregate after the specified version.
* Author:       Tom Mooney
* Date Created: 2018MAR20
*
* History: 2018MAR20 - tmooney - Created procedure
*
**************************************************************************************************/
CREATE procedure [dbo].[GetEventsForAggregate]
(
    @AggregateId [varchar] (255) ,
    @ApplicationName [varchar] (255) ,
    @AggregateType [varchar] (255) ,
    @FromVersion [int] = 1
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.EventData
    FROM dbo.Event e
    INNER JOIN dbo.Aggregate ag ON e.AggregateKey = ag.AggregateKey 
    WHERE ag.AggregateId = @AggregateId 
        AND e.[Version] > @FromVersion
    ORDER BY e.[Version] ASC
END;
GO
GRANT EXECUTE ON  [dbo].[GetEventsForAggregate] TO [lc_ReaderWriter]
GO
