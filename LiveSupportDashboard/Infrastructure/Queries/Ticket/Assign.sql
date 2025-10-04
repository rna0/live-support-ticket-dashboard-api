UPDATE tickets
SET assigned_agent_id = @agentId, updated_at = @now
WHERE id = @id

