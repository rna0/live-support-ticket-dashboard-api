UPDATE sessions
SET status = @status, updated_at = @now
WHERE id = @id

