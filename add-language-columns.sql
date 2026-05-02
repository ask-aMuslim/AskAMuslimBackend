SET QUOTED_IDENTIFIER ON;
GO

-- Add Language columns to course content tables.
-- Existing rows are backfilled to English so the columns can be added safely.

IF COL_LENGTH('dbo.Levels', 'Language') IS NULL
BEGIN
    ALTER TABLE dbo.Levels
    ADD Language NVARCHAR(50) NOT NULL
        CONSTRAINT DF_Levels_Language DEFAULT (N'English') WITH VALUES;
END
GO

IF COL_LENGTH('dbo.Courses', 'Language') IS NULL
BEGIN
    ALTER TABLE dbo.Courses
    ADD Language NVARCHAR(50) NOT NULL
        CONSTRAINT DF_Courses_Language DEFAULT (N'English') WITH VALUES;
END
GO

IF COL_LENGTH('dbo.Lessons', 'Language') IS NULL
BEGIN
    ALTER TABLE dbo.Lessons
    ADD Language NVARCHAR(50) NOT NULL
        CONSTRAINT DF_Lessons_Language DEFAULT (N'English') WITH VALUES;
END
GO

IF COL_LENGTH('dbo.QAs', 'Language') IS NULL
BEGIN
    ALTER TABLE dbo.QAs
    ADD Language NVARCHAR(50) NOT NULL
        CONSTRAINT DF_QAs_Language DEFAULT (N'English') WITH VALUES;
END
GO