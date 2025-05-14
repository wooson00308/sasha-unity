"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const { McpServer } = require("@modelcontextprotocol/sdk/server/mcp.js");
const { StdioServerTransport } = require("@modelcontextprotocol/sdk/server/stdio.js");
// const { ErrorCode, McpError, CallToolRequestSchema, ListToolsRequestSchema } = require("@modelcontextprotocol/sdk/ ??? .js"); // Original, commented out
const packageJson = require('../package.json');
const { registerExcelTools } = require("./excelTools");
async function main() {
    const server = new McpServer({
        name: packageJson.name,
        version: packageJson.version,
        displayName: "AF Data Tool Server",
        description: "A server to provide tools for AF_Data.xlsx management."
        // capabilities: {} // 필요하다면 추가
    });
    // Note: mcp-image-server does not explicitly set capabilities in the constructor.
    // If v1.10.2 requires it, it should be: server.setCapabilities({ tools: {} }); or similar.
    // For now, assuming it's not strictly needed at construction if tools are added via server.tool()
    registerExcelTools(server);
    const transport = new StdioServerTransport();
    await server.connect(transport);
    console.error("MCP server '" + packageJson.name + "' connected via stdio.");
    process.stdin.resume();
}
main().catch((err) => {
    console.error("Failed to start server:", err);
    process.exit(1);
});
//# sourceMappingURL=server.js.map