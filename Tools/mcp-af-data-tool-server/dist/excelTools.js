"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const { registerGetExcelTools } = require('./excelGetTools');
const { registerUpdateExcelTools } = require('./excelUpdateTools');
/**
 * Registers Excel-related tools with the MCP server.
 * @param {any} server MCP Server instance
 */
function registerExcelTools(server) {
    console.error("[ExcelTools] Registering Excel tools...");
    registerGetExcelTools(server);
    registerUpdateExcelTools(server);
    // If we add other categories of tools (e.g., delete),
    // their registration functions would be called here as well.
    // e.g., registerDeleteExcelTools(server);
    console.error("[ExcelTools] Excel tools registration complete.");
}
module.exports = { registerExcelTools };
//# sourceMappingURL=excelTools.js.map