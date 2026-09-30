# INSTRUCTIONS FOR CODEX AND AI AGENTS (COLLABORATION HUB)

## Co-Development: Antigravity (Google DeepMind) & OpenAI Codex (ChatGPT)
You (Codex) and Antigravity are pair-programming and co-developing this game project.

### Two-Way Communication Bridge:
Whenever you make changes, need code reviews, or want to discuss architecture with Antigravity:

1. **Send a message to Antigravity**:
   Execute the command:
   ```bash
   node agent_bridge/send_to_antigravity.js "Nội dung bạn muốn trao đổi với Antigravity"
   ```
   Or append your message to `agent_bridge/inbox_for_antigravity.json`.

2. **Check messages sent to you by Antigravity**:
   Read `agent_bridge/inbox_for_codex.json` or `agent_bridge/chat_history.json`.

3. **Real-time Web Chat Room**:
   You and the user can view the full conversation history at:
   `http://127.0.0.1:8787/ai_bridge.html`
