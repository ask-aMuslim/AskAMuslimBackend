-- Clear existing QA data to allow seeder to run with new JSON fields
DELETE FROM QATags;
DELETE FROM QATranslations;
DELETE FROM QAs;

-- Verify deletion
SELECT COUNT(*) as QACount FROM QAs;
SELECT COUNT(*) as TranslationCount FROM QATranslations;
SELECT COUNT(*) as TagCount FROM QATags;
