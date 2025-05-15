const { z } = require("zod");
const ExcelJS = require('exceljs');
const path = require('path');
import type { CellValue, Worksheet } from 'exceljs';

const DEFAULT_EXCEL_FILE_PATH = path.join(__dirname, "../../../Assets/AF/Data/AF_Data.xlsx");

// Schema for the create_new_entity tool
const createNewEntitySchema = {
    sheetName: z.string().describe("The name of the sheet where the new entity (row) will be added (e.g., 'Parts', 'Weapons')."),
    entityData: z.record(z.any()).describe("An object representing the new entity. Keys should be column headers, and values will be the cell values for the new row."),
    filePath: z.string().optional().describe("Optional path to the Excel file. Defaults to AF_Data.xlsx relative to project root.")
};

/**
 * Registers CREATE Excel-related tools with the MCP server.
 * @param {any} server MCP Server instance
 */
function registerCreateExcelTools(server: any) {
    console.error("[ExcelCreateTools] Registering CREATE Excel tools...");

    server.tool(
        "create_new_entity",
        createNewEntitySchema,
        async (args: {
            sheetName: string,
            entityData: Record<string, any>,
            filePath?: string
        }) => {
            console.error(`[ExcelCreateTools] Tool 'create_new_entity' called with args: ${JSON.stringify(args)}`);
            const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
            const workbook = new ExcelJS.Workbook();

            try {
                // Try to read the existing workbook, or create a new one if it doesn't exist (though for this tool, it should usually exist)
                try {
                    await workbook.xlsx.readFile(excelFilePath);
                } catch (readError: any) {
                    // If the file doesn't exist, this tool probably shouldn't create it from scratch.
                    // It's designed to add a row to an existing sheet.
                    console.error(`[ExcelCreateTools] Error reading workbook at ${excelFilePath}: ${readError.message}`);
                    return { isError: true, content: [{ type: "text", text: `Error reading workbook '${excelFilePath}': ${readError.message}. Ensure the file exists.` }] };
                }
                
                const worksheet: Worksheet | undefined = workbook.getWorksheet(args.sheetName);

                if (!worksheet) {
                    return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' not found in '${excelFilePath}'. New entity cannot be added.` }] };
                }

                // Get header row to map entityData keys to columns correctly
                const headerRow = worksheet.getRow(1);
                const headerValues: string[] = [];
                headerRow.eachCell((cell, colNumber) => {
                    headerValues[colNumber -1] = cell.value ? cell.value.toString().trim() : '';
                });
                
                if (headerValues.length === 0) {
                     return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' has no header row or is empty. Cannot determine columns for new entity.` }] };
                }

                const newRowValues: any[] = new Array(headerValues.length).fill(null);

                for (const key in args.entityData) {
                    const columnIndex = headerValues.indexOf(key);
                    if (columnIndex !== -1) {
                        newRowValues[columnIndex] = args.entityData[key];
                    } else {
                        console.warn(`[ExcelCreateTools] Column '${key}' from entityData not found in sheet '${args.sheetName}' headers. This field will be ignored.`);
                    }
                }
                
                // Add the new row with mapped values
                worksheet.addRow(newRowValues);

                await workbook.xlsx.writeFile(excelFilePath);

                return { content: [{ type: "text", text: `Successfully added new entity to sheet '${args.sheetName}'.` }] };

            } catch (error: any) {
                console.error(`[ExcelCreateTools] Error in create_new_entity for sheet '${args.sheetName}': ${error.message}`);
                console.error(error.stack); // Log stack for more details
                return { isError: true, content: [{ type: "text", text: `Error processing create_new_entity: ${error.message}` }] };
            }
        }
    );

    console.error("[ExcelCreateTools] CREATE Excel tools registration complete.");
}

module.exports = { registerCreateExcelTools }; 