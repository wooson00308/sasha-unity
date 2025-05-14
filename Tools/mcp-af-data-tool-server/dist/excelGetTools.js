"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const { z } = require("zod");
const ExcelJS = require('exceljs');
const path = require('path');
const DEFAULT_EXCEL_FILE_PATH = path.join(__dirname, "../../../Assets/AF/Data/AF_Data.xlsx");
// Schemas
const getSheetNamesSchema = {
    filePath: z.string().optional().describe("Optional path to the Excel file. Defaults to AF_Data.xlsx relative to project root.")
};
const readSheetDataSchema = {
    sheetName: z.string().describe("The name of the sheet to read data from."),
    filePath: z.string().optional().describe("Optional path to the Excel file. Defaults to configured path.")
};
const getEntityDetailsSchema = {
    sheetName: z.string().describe("The name of the sheet to search in (e.g., 'Frames', 'Parts')."),
    entityId: z.string().describe("The ID of the entity to find (e.g., 'FRM_LIGHT_01')."),
    idColumnName: z.string().describe("The name of the column containing the ID (e.g., 'FrameID', 'PartID')."),
    filePath: z.string().optional().describe("Optional path to the Excel file. Defaults to configured path.")
};
/**
 * Registers GET Excel-related tools with the MCP server.
 * @param {any} server MCP Server instance
 */
function registerGetExcelTools(server) {
    console.error("[ExcelGetTools] Registering GET Excel tools...");
    server.tool("get_sheet_names", getSheetNamesSchema, async (args) => {
        console.error(`[ExcelGetTools] Tool 'get_sheet_names' called with args: ${JSON.stringify(args)}`);
        const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
        try {
            const workbook = new ExcelJS.Workbook();
            await workbook.xlsx.readFile(excelFilePath);
            const sheetNames = workbook.worksheets.map((ws) => ws.name);
            return { content: [{ type: "text", text: JSON.stringify(sheetNames) }] };
        }
        catch (error) {
            console.error(`[ExcelGetTools] Error in get_sheet_names for file '${excelFilePath}': ${error.message}`);
            return { isError: true, content: [{ type: "text", text: `Error getting sheet names from '${excelFilePath}': ${error.message}` }] };
        }
    });
    server.tool("read_sheet_data", readSheetDataSchema, async (args) => {
        console.error(`[ExcelGetTools] Tool 'read_sheet_data' called with args: ${JSON.stringify(args)}`);
        const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
        try {
            const workbook = new ExcelJS.Workbook();
            await workbook.xlsx.readFile(excelFilePath);
            const worksheet = workbook.getWorksheet(args.sheetName);
            if (!worksheet) {
                return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' not found in '${excelFilePath}'.` }] };
            }
            const jsonData = [];
            const headerRow = worksheet.getRow(1).values;
            worksheet.eachRow((row, rowNumber) => {
                if (rowNumber > 1) {
                    let rowData = {};
                    row.values.forEach((value, index) => {
                        const headerName = headerRow[index] ? headerRow[index].toString() : `column${index}`;
                        rowData[headerName] = value;
                    });
                    jsonData.push(rowData);
                }
            });
            return { content: [{ type: "text", text: JSON.stringify(jsonData, null, 2) }] };
        }
        catch (error) {
            console.error(`[ExcelGetTools] Error in read_sheet_data for sheet '${args.sheetName}' in file '${excelFilePath}': ${error.message}`);
            return { isError: true, content: [{ type: "text", text: `Error reading sheet '${args.sheetName}' from '${excelFilePath}': ${error.message}` }] };
        }
    });
    server.tool("get_entity_details", getEntityDetailsSchema, async (args) => {
        console.error(`[ExcelGetTools] Tool 'get_entity_details' called with args: ${JSON.stringify(args)}`);
        const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
        try {
            const workbook = new ExcelJS.Workbook();
            await workbook.xlsx.readFile(excelFilePath);
            const worksheet = workbook.getWorksheet(args.sheetName);
            if (!worksheet) {
                return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' not found in '${excelFilePath}'.` }] };
            }
            const headerRow = worksheet.getRow(1).values;
            if (!headerRow || headerRow.length === 0) {
                return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' has no header row or is empty.` }] };
            }
            const idColIndex = headerRow.findIndex(header => header && header.toString() === args.idColumnName);
            if (idColIndex === -1) {
                return { isError: true, content: [{ type: "text", text: `ID column '${args.idColumnName}' not found in sheet '${args.sheetName}'.` }] };
            }
            let foundEntityData = null;
            worksheet.eachRow((row, rowNumber) => {
                if (rowNumber > 1) {
                    const rowValues = row.values;
                    if (rowValues[idColIndex] && rowValues[idColIndex].toString() === args.entityId) {
                        foundEntityData = {};
                        headerRow.forEach((header, index) => {
                            if (header) {
                                foundEntityData[header.toString()] = rowValues[index];
                            }
                        });
                        return false;
                    }
                }
            });
            if (foundEntityData) {
                return { content: [{ type: "text", text: JSON.stringify(foundEntityData, null, 2) }] };
            }
            else {
                return { content: [{ type: "text", text: `Entity with ID '${args.entityId}' not found in column '${args.idColumnName}' of sheet '${args.sheetName}'.` }] };
            }
        }
        catch (error) {
            console.error(`[ExcelGetTools] Error in get_entity_details for ID '${args.entityId}' in sheet '${args.sheetName}': ${error.message}`);
            return { isError: true, content: [{ type: "text", text: `Error processing get_entity_details: ${error.message}` }] };
        }
    });
    console.error("[ExcelGetTools] GET Excel tools registration complete.");
}
module.exports = { registerGetExcelTools };
//# sourceMappingURL=excelGetTools.js.map