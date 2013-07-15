USE [master]
GO
/****** Object:  Table [dbo].[MasterRecords]    Script Date: 07/14/2013 00:56:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT name FROM sys.databases WHERE name = N'Test.Client')
DROP DATABASE [Test.Client]
GO

/****** Object:  Database [Test.Client]    Script Date: 07/14/2013 00:55:22 ******/
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
/****** Object:  Table [dbo].[DetailsRecords]    Script Date: 07/14/2013 00:56:23 ******/
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
/****** Object:  ForeignKey [FK_dbo.DetailsRecords_dbo.MasterRecords_MasterRecordId]    Script Date: 07/14/2013 00:56:23 ******/
ALTER TABLE [dbo].[DetailsRecords]  WITH CHECK ADD  CONSTRAINT [FK_dbo.DetailsRecords_dbo.MasterRecords_MasterRecordId] FOREIGN KEY([MasterRecordId])
REFERENCES [dbo].[MasterRecords] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DetailsRecords] CHECK CONSTRAINT [FK_dbo.DetailsRecords_dbo.MasterRecords_MasterRecordId]
GO
