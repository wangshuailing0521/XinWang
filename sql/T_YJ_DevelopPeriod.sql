CREATE TABLE T_YJ_DevelopPeriod(
	FID INT IDENTITY(1,1),
	FCompanyID VARCHAR(255),
	FDate DATETIME)

INSERT INTO T_YJ_DevelopPeriod(FCompanyID,FDate)SELECT 1,'2023-05-25'