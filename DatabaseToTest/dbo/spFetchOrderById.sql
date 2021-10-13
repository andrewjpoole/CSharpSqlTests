CREATE PROCEDURE [dbo].[spFetchOrderById]
	@orderId int = 0
AS
	SELECT Id, Customers_Id, DateCreated, DateFulfilled, DatePaid, ProductName, Quantity, QuotedPrice, Notes
	FROM Orders
	WHERE Id = @orderId

