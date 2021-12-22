CREATE TABLE [dbo].[Orders]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [DateCreated] DATE NOT NULL, 
    [Customers_Id] INT NOT NULL, 
    [DateFulfilled] DATE NULL, 
    [DatePaid] DATE NULL, 
    [ProductName] NVARCHAR(500) NOT NULL, 
    [Quantity] INT NOT NULL, 
    [QuotedPrice] SMALLMONEY NOT NULL, 
    [Notes] NVARCHAR(MAX) NULL, 
    CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([Customers_Id]) REFERENCES [Customers]([Id])
)
