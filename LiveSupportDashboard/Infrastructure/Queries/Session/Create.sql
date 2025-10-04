INSERT INTO sessions (id, user_id, assigned_agent_id, status, metadata, created_at, updated_at, last_activity_at)
VALUES (@id, @user_id, @assigned_agent_id, @status, @metadata::jsonb, @now, @now, @now)

