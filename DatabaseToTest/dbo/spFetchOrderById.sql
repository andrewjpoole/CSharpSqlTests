CREATE PROCEDURE [dbo].[spFetchOrderById]
	@orderId int = 0
AS
	SELECT * 
	FROM Orders
	WHERE Id = @orderId

