SELECT id, user_id, assigned_agent_id, status, metadata, created_at, updated_at, last_activity_at
FROM sessions
WHERE user_id = @user_id
ORDER BY created_at DESC

