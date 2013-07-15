USE [master]



GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT name FROM sys.databases WHERE name = N'Test.Client')
DROP DATABASE [Test.Client]
GO

CREATE DATABASE [Test.Client] 
GO

USE [Test.Client]
GO

CREATE TABLE [dbo].[MasterRecords](
	[Id] [BigInt] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_dbo.MasterRecords] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DetailsRecords](
	[MasterRecordId] [BigInt] NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
 CONSTRAINT [PK_dbo.DetailsRecords] PRIMARY KEY CLUSTERED 
(
	[MasterRecordId] ASC,
	[Name] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DetailsRecords]  WITH CHECK ADD  CONSTRAINT [FK_dbo.DetailsRecords_dbo.MasterRecords_MasterRecordId] FOREIGN KEY([MasterRecordId])
REFERENCES [dbo].[MasterRecords] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DetailsRecords] CHECK CONSTRAINT [FK_dbo.DetailsRecords_dbo.MasterRecords_MasterRecordId]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fnSplit]'))
DROP FUNCTION [dbo].[fnSplit]
GO

create function [dbo].[fnSplit] (@str varchar(max))
returns @tbl table (id int, name varchar(max))
as
begin
declare @i int, @j int, @delim char(1), @ind int, @line varchar(max) 
	select @i = 1, @delim = Char(13)
	while @i <= len(@str)
	begin
		select @j = charindex(@delim, @str, @i)
		if @j = 0
		begin
			select @j = len(@str) + 1
		end
		select @line = substring(@str, @i, @j - @i)
		select @ind = charindex(',', @line, 1)

		insert @tbl values (left(@line, @ind - 1), substring(@line, @ind + 2, @j - @i - @ind - 2))
		select @i = @j + 2 -- skip char(13) + char(10)
	end
	return
end
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[spUpsertMaster]'))
DROP PROCEDURE [dbo].[spUpsertMaster]
GO

create procedure [dbo].[spUpsertMaster] @str varchar(max)
as
begin
   set nocount on;

   set IDENTITY_INSERT MasterRecords ON
	
   merge into MasterRecords as t
   using (select distinct(Id),Name from dbo.fnSplit(@str)) as s
   on (t.Id = s.Id)
   when matched then update set t.Name = s.Name
   when not matched then insert (Id, Name) values (s.Id, s.Name)
   output s.Id, s.Name, $action as Action;
      
   set IDENTITY_INSERT MasterRecords OFF
end
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[spUpsertDetails]'))
DROP PROCEDURE [dbo].[spUpsertDetails]
GO

create procedure [dbo].[spUpsertDetails] @str varchar(max)
as
begin
   set nocount on;

   declare @source table (Id int, Name nvarchar(max));
   declare @mergeRes table (Id int, Name nvarchar(max), Action nvarchar(max));
   insert into @source select distinct(Id),Name from dbo.fnSplit(@str)

   merge into DetailsRecords as t
   using (select * from @source) as s
   on t.MasterRecordId = s.Id and t.Name = s.Name
   when matched then update set t.Name = s.Name
   when not matched and exists (select * from MasterRecords where Id = s.Id) then insert (MasterRecordId, Name) values (s.Id, s.Name)
   output s.Id, s.Name, $action as Action into @mergeRes;
      
   select s.Id, s.Name, isnull(Action, 'NONE') as Action from
   @source s
   left join @mergeRes mr on s.Id = mr.Id
end
GO

