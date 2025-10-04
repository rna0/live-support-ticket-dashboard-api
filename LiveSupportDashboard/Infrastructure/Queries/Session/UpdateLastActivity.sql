UPDATE sessions
SET last_activity_at = @now, updated_at = @now
WHERE id = @id

