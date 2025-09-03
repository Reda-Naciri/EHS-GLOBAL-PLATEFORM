-- Manual migration to add Applications table
-- Run this script on your database

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Applications' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Applications] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [Icon] nvarchar(10) NOT NULL,
        [RedirectUrl] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [Order] int NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Applications] PRIMARY KEY ([Id])
    );

    -- Insert seed data
    INSERT INTO [dbo].[Applications] ([Title], [Icon], [RedirectUrl], [IsActive], [Order], [CreatedAt])
    VALUES 
        ('Chemical Product', '🧪', 'http://162.109.85.69:778/app/products', 1, 1, '2025-01-01T00:00:00.0000000Z'),
        ('App 2', '🖥️', '#', 1, 2, '2025-01-01T00:00:00.0000000Z'),
        ('App 3', '💼', '#', 1, 3, '2025-01-01T00:00:00.0000000Z'),
        ('App 4', '📝', '#', 1, 4, '2025-01-01T00:00:00.0000000Z');

    PRINT 'Applications table created and seed data inserted.';
END
ELSE
BEGIN
    PRINT 'Applications table already exists.';
END