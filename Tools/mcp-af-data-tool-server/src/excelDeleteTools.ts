const { z } = require("zod");
const ExcelJS = require('exceljs');
const path = require('path');
import type { Worksheet, CellValue } from 'exceljs';

const DEFAULT_EXCEL_FILE_PATH = path.join(__dirname, "../../../Assets/AF/Data/AF_Data.xlsx");

// Schema for the delete_entity tool
const deleteEntitySchema = {
    sheetName: z.string().describe("The name of the sheet from which the entity (row) will be deleted."),
    entityId: z.string().describe("The ID of the entity (row) to delete."),
    idColumnName: z.string().describe("The name of the column containing the entity ID."),
    filePath: z.string().optional().describe("Optional path to the Excel file. Defaults to AF_Data.xlsx relative to project root.")
};

/**
 * Registers DELETE Excel-related tools with the MCP server.
 * @param {any} server MCP Server instance
 */
function registerDeleteExcelTools(server: any) {
    console.error("[ExcelDeleteTools] Registering DELETE Excel tools...");

    server.tool(
        "delete_entity",
        deleteEntitySchema,
        async (args: {
            sheetName: string,
            entityId: string,
            idColumnName: string,
            filePath?: string
        }) => {
            console.error(`[ExcelDeleteTools] Tool 'delete_entity' called with args: ${JSON.stringify(args)}`);
            const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
            const workbook = new ExcelJS.Workbook();

            try {
                await workbook.xlsx.readFile(excelFilePath);
                const worksheet: Worksheet | undefined = workbook.getWorksheet(args.sheetName);

                if (!worksheet) {
                    return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' not found in '${excelFilePath}'.` }] };
                }

                const headerRow = worksheet.getRow(1);
                if (!headerRow.values || (headerRow.values as CellValue[]).length === 0) {
                    return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' has no header row or is empty.` }] };
                }

                let idColNumber = -1;
                headerRow.eachCell((cell, colNumber) => {
                    if (cell.value && cell.value.toString().trim() === args.idColumnName.trim()) {
                        idColNumber = colNumber;
                    }
                });

                if (idColNumber === -1) {
                    return { isError: true, content: [{ type: "text", text: `ID column '${args.idColumnName}' not found in sheet '${args.sheetName}'.` }] };
                }

                let targetRowNumber = -1;
                for (let i = 2; i <= worksheet.rowCount; i++) {
                    const row = worksheet.getRow(i);
                    const cellValue = row.getCell(idColNumber).value;
                    if (cellValue && cellValue.toString().trim() === args.entityId.trim()) {
                        targetRowNumber = i;
                        break;
                    }
                }

                if (targetRowNumber === -1) {
                    return { isError: true, content: [{ type: "text", text: `Entity ID '${args.entityId}' not found in column '${args.idColumnName}' of sheet '${args.sheetName}'.` }] };
                }

                // Remove the row
                worksheet.spliceRows(targetRowNumber, 1);

                await workbook.xlsx.writeFile(excelFilePath);

                return { content: [{ type: "text", text: `Successfully deleted entity '${args.entityId}' from sheet '${args.sheetName}'.` }] };

            } catch (error: any) {
                console.error(`[ExcelDeleteTools] Error in delete_entity for ID '${args.entityId}' in sheet '${args.sheetName}': ${error.message}`);
                console.error(error.stack);
                return { isError: true, content: [{ type: "text", text: `Error processing delete_entity: ${error.message}` }] };
            }
        }
    );

    console.error("[ExcelDeleteTools] DELETE Excel tools registration complete.");
}

module.exports = { registerDeleteExcelTools }; 