-- database.sql

-- DROP SCHEMA dbo;

-- ProjectPurrDB.dbo.AppointmentGroups definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.AppointmentGroups;

CREATE TABLE ProjectPurrDB.dbo.AppointmentGroups (
	GroupID int IDENTITY(1,1) NOT NULL,
	GroupTime datetime2 NOT NULL,
	Notes nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS DEFAULT N'' NOT NULL,
	CreatedAt datetime2 NOT NULL,
	FinalizedAt datetime2 NULL,
	Status nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS DEFAULT N'' NOT NULL,
	CONSTRAINT PK_AppointmentGroups PRIMARY KEY (GroupID)
);


-- ProjectPurrDB.dbo.Notifications definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.Notifications;

CREATE TABLE ProjectPurrDB.dbo.Notifications (
	NotificationID int IDENTITY(1,1) NOT NULL,
	Message nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Type] nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CreatedAt datetime2 NOT NULL,
	IsRead bit NOT NULL,
	RedirectUrl nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TargetRole nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TargetUserId int NULL,
	CONSTRAINT PK_Notifications PRIMARY KEY (NotificationID)
);


-- ProjectPurrDB.dbo.ServiceCategories definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.ServiceCategories;

CREATE TABLE ProjectPurrDB.dbo.ServiceCategories (
	CategoryID int IDENTITY(1,1) NOT NULL,
	ServiceType nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Description nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_ServiceCategories PRIMARY KEY (CategoryID)
);


-- ProjectPurrDB.dbo.SystemLogs definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.SystemLogs;

CREATE TABLE ProjectPurrDB.dbo.SystemLogs (
	LogID int IDENTITY(1,1) NOT NULL,
	ActionType nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	PerformedBy nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Timestamp] datetime2 NOT NULL,
	Description nvarchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Module nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_SystemLogs PRIMARY KEY (LogID)
);


-- ProjectPurrDB.dbo.Users definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.Users;

CREATE TABLE ProjectPurrDB.dbo.Users (
	UserID int IDENTITY(1,1) NOT NULL,
	FirstName nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	LastName nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Email nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Phone nvarchar(12) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Password nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Type] nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Status nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CreatedAt datetime2 NOT NULL,
	ProfileImage nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	ResetToken nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TokenExpiry datetime2 NULL,
	FailedLoginAttempts int DEFAULT 0 NOT NULL,
	LastTwoFactorVerification datetime2 NULL,
	LockoutEnd datetime2 NULL,
	TwoFactorCode nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TwoFactorEnabled bit DEFAULT CONVERT([bit],(0)) NOT NULL,
	TwoFactorExpiry datetime2 NULL,
	LastLoginDevice nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LastLoginIP nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Users PRIMARY KEY (UserID)
);


-- ProjectPurrDB.dbo.[__EFMigrationsHistory] definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.[__EFMigrationsHistory];

CREATE TABLE ProjectPurrDB.dbo.[__EFMigrationsHistory] (
	MigrationId nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	ProductVersion nvarchar(32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
);


-- ProjectPurrDB.dbo.MicrosoftAccountConnections definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.MicrosoftAccountConnections;

CREATE TABLE ProjectPurrDB.dbo.MicrosoftAccountConnections (
	Id int IDENTITY(1,1) NOT NULL,
	UserID int NOT NULL,
	MicrosoftEmail nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AccessToken nvarchar(2000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RefreshToken nvarchar(2000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TokenExpiry datetime2 NULL,
	ConnectedAt datetime2 NOT NULL,
	IsAutoSyncEnabled bit DEFAULT CONVERT([bit],(0)) NOT NULL,
	CONSTRAINT PK_MicrosoftAccountConnections PRIMARY KEY (Id),
	CONSTRAINT FK_MicrosoftAccountConnections_Users_UserID FOREIGN KEY (UserID) REFERENCES ProjectPurrDB.dbo.Users(UserID) ON DELETE CASCADE
);
 CREATE NONCLUSTERED INDEX IX_MicrosoftAccountConnections_UserID ON ProjectPurrDB.dbo.MicrosoftAccountConnections (  UserID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- ProjectPurrDB.dbo.Owners definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.Owners;

CREATE TABLE ProjectPurrDB.dbo.Owners (
	OwnerID int IDENTITY(1,1) NOT NULL,
	UserID int NOT NULL,
	Name nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Email nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Phone nvarchar(12) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_Owners PRIMARY KEY (OwnerID),
	CONSTRAINT FK_Owners_Users_UserID FOREIGN KEY (UserID) REFERENCES ProjectPurrDB.dbo.Users(UserID) ON DELETE CASCADE
);
 CREATE UNIQUE NONCLUSTERED INDEX IX_Owners_UserID ON ProjectPurrDB.dbo.Owners (  UserID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- ProjectPurrDB.dbo.Pets definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.Pets;

CREATE TABLE ProjectPurrDB.dbo.Pets (
	PetID int IDENTITY(1,1) NOT NULL,
	OwnerID int NOT NULL,
	Name nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Type] nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Breed nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CreatedAt datetime2 NOT NULL,
	Birthdate datetime2 NOT NULL,
	PhotoPath nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Pets PRIMARY KEY (PetID),
	CONSTRAINT FK_Pets_Owners_OwnerID FOREIGN KEY (OwnerID) REFERENCES ProjectPurrDB.dbo.Owners(OwnerID) ON DELETE CASCADE
);
 CREATE NONCLUSTERED INDEX IX_Pets_OwnerID ON ProjectPurrDB.dbo.Pets (  OwnerID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- ProjectPurrDB.dbo.RefreshTokens definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.RefreshTokens;

CREATE TABLE ProjectPurrDB.dbo.RefreshTokens (
	Id int IDENTITY(1,1) NOT NULL,
	UserID int NOT NULL,
	Token nvarchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	ExpiresAt datetime2 NOT NULL,
	CreatedAt datetime2 NOT NULL,
	RevokedAt datetime2 NULL,
	ReplacedByToken nvarchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DeviceInfo nvarchar(200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
	CONSTRAINT FK_RefreshTokens_Users_UserID FOREIGN KEY (UserID) REFERENCES ProjectPurrDB.dbo.Users(UserID) ON DELETE CASCADE
);
 CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserID ON ProjectPurrDB.dbo.RefreshTokens (  UserID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- ProjectPurrDB.dbo.ServiceSubtypes definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.ServiceSubtypes;

CREATE TABLE ProjectPurrDB.dbo.ServiceSubtypes (
	SubtypeID int IDENTITY(1,1) NOT NULL,
	CategoryID int NOT NULL,
	ServiceSubType nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Description nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_ServiceSubtypes PRIMARY KEY (SubtypeID),
	CONSTRAINT FK_ServiceSubtypes_ServiceCategories_CategoryID FOREIGN KEY (CategoryID) REFERENCES ProjectPurrDB.dbo.ServiceCategories(CategoryID) ON DELETE CASCADE
);
 CREATE NONCLUSTERED INDEX IX_ServiceSubtypes_CategoryID ON ProjectPurrDB.dbo.ServiceSubtypes (  CategoryID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- ProjectPurrDB.dbo.AppointmentDrafts definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.AppointmentDrafts;

CREATE TABLE ProjectPurrDB.dbo.AppointmentDrafts (
	DraftID int IDENTITY(1,1) NOT NULL,
	UserID int NULL,
	OwnerID int NULL,
	PetID int NULL,
	CategoryID int NULL,
	SubtypeID int NULL,
	AppointmentDate datetime2 NOT NULL,
	AppointmentTime nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Notes nvarchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CreatedAt datetime2 NOT NULL,
	GroupDraftId nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_AppointmentDrafts PRIMARY KEY (DraftID),
	CONSTRAINT FK_AppointmentDrafts_Pets_PetID FOREIGN KEY (PetID) REFERENCES ProjectPurrDB.dbo.Pets(PetID),
	CONSTRAINT FK_AppointmentDrafts_ServiceCategories_CategoryID FOREIGN KEY (CategoryID) REFERENCES ProjectPurrDB.dbo.ServiceCategories(CategoryID),
	CONSTRAINT FK_AppointmentDrafts_ServiceSubtypes_SubtypeID FOREIGN KEY (SubtypeID) REFERENCES ProjectPurrDB.dbo.ServiceSubtypes(SubtypeID)
);
 CREATE NONCLUSTERED INDEX IX_AppointmentDrafts_CategoryID ON ProjectPurrDB.dbo.AppointmentDrafts (  CategoryID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX IX_AppointmentDrafts_PetID ON ProjectPurrDB.dbo.AppointmentDrafts (  PetID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX IX_AppointmentDrafts_SubtypeID ON ProjectPurrDB.dbo.AppointmentDrafts (  SubtypeID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- ProjectPurrDB.dbo.Appointments definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.Appointments;

CREATE TABLE ProjectPurrDB.dbo.Appointments (
	AppointmentID int IDENTITY(1,1) NOT NULL,
	PetID int NOT NULL,
	GroupID int NULL,
	AppointmentDate datetime2 NOT NULL,
	CategoryID int NULL,
	SubtypeID int NULL,
	Status nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CreatedAt datetime2 NOT NULL,
	AdministeredBy nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Notes nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsSynced bit DEFAULT CONVERT([bit],(0)) NOT NULL,
	DueDate datetime2 NULL,
	EmailSentToday int DEFAULT 0 NOT NULL,
	LastEmailSentAt datetime2 NULL,
	LastSmsSentAt datetime2 NULL,
	ReminderCounterDate datetime2 NULL,
	SmsSentToday int DEFAULT 0 NOT NULL,
	CONSTRAINT PK_Appointments PRIMARY KEY (AppointmentID),
	CONSTRAINT FK_Appointments_AppointmentGroups_GroupID FOREIGN KEY (GroupID) REFERENCES ProjectPurrDB.dbo.AppointmentGroups(GroupID) ON DELETE CASCADE,
	CONSTRAINT FK_Appointments_Pets_PetID FOREIGN KEY (PetID) REFERENCES ProjectPurrDB.dbo.Pets(PetID) ON DELETE CASCADE,
	CONSTRAINT FK_Appointments_ServiceCategories_CategoryID FOREIGN KEY (CategoryID) REFERENCES ProjectPurrDB.dbo.ServiceCategories(CategoryID) ON DELETE SET NULL,
	CONSTRAINT FK_Appointments_ServiceSubtypes_SubtypeID FOREIGN KEY (SubtypeID) REFERENCES ProjectPurrDB.dbo.ServiceSubtypes(SubtypeID)
);
 CREATE NONCLUSTERED INDEX IX_Appointments_CategoryID ON ProjectPurrDB.dbo.Appointments (  CategoryID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX IX_Appointments_GroupID ON ProjectPurrDB.dbo.Appointments (  GroupID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX IX_Appointments_PetID ON ProjectPurrDB.dbo.Appointments (  PetID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX IX_Appointments_SubtypeID ON ProjectPurrDB.dbo.Appointments (  SubtypeID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- ProjectPurrDB.dbo.PetCards definition

-- Drop table

-- DROP TABLE ProjectPurrDB.dbo.PetCards;

CREATE TABLE ProjectPurrDB.dbo.PetCards (
	PetCardID int IDENTITY(1,1) NOT NULL,
	AppointmentID int NOT NULL,
	DateAdministered datetime2 NOT NULL,
	NextDueDate datetime2 NULL,
	CONSTRAINT PK_PetCards PRIMARY KEY (PetCardID),
	CONSTRAINT FK_PetCards_Appointments_AppointmentID FOREIGN KEY (AppointmentID) REFERENCES ProjectPurrDB.dbo.Appointments(AppointmentID) ON DELETE CASCADE
);
 CREATE NONCLUSTERED INDEX IX_PetCards_AppointmentID ON ProjectPurrDB.dbo.PetCards (  AppointmentID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
