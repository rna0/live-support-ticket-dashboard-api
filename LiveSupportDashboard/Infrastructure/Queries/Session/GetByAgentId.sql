SELECT id, user_id, assigned_agent_id, status, metadata, created_at, updated_at, last_activity_at
FROM sessions
WHERE assigned_agent_id = @agent_id
ORDER BY last_activity_at DESC

