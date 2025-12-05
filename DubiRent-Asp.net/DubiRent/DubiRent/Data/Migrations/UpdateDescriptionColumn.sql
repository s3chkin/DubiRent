-- Update Description column to nvarchar(MAX) - no length limit
ALTER TABLE [Properties]
ALTER COLUMN [Description] nvarchar(MAX) NOT NULL;

