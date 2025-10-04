SELECT id, ticket_id, action, details, agent_name, created_at
FROM ticket_history
WHERE ticket_id = @ticketId
ORDER BY created_at ASC

