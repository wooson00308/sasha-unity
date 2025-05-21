const { z } = require("zod");
const ExcelJS = require('exceljs');
const path = require('path');

// Import ExcelJS types for better type checking
// Adjusted imports based on typical exceljs structure for rich text
import type { CellValue, Row, Worksheet, CellRichTextValue, Font, RichText } from 'exceljs';

const DEFAULT_EXCEL_FILE_PATH = path.join(__dirname, "../../../Assets/AF/Data/AF_Data.xlsx");

// Schema for the update_entity_stat tool
const updateEntityStatSchema = {
    sheetName: z.string().describe("The name of the sheet to update (e.g., 'Parts', 'Frames')."),
    entityId: z.string().describe("The ID of the entity (row) to update (e.g., 'PT_Heavy_Laser_01')."),
    idColumnName: z.string().describe("The name of the column containing the entity ID (e.g., 'PartID', 'FrameID')."),
    statColumnName: z.string().describe("The name of the column (stat) to update."),
    newValue: z.string().describe("The new value for the stat. Use an empty string \"\" to clear the cell."),
    filePath: z.string().optional().describe("Optional path to the Excel file. Defaults to AF_Data.xlsx relative to project root.")
};

/**
 * Registers UPDATE Excel-related tools with the MCP server.
 * @param {any} server MCP Server instance
 */
function registerUpdateExcelTools(server: any) {
    console.error("[ExcelUpdateTools] Registering UPDATE Excel tools...");

    server.tool(
        "update_entity_stat",
        updateEntityStatSchema,
        async (args: { 
            sheetName: string, 
            entityId: string, 
            idColumnName: string, 
            statColumnName: string, 
            newValue: string, 
            filePath?: string 
        }) => {
            console.error(`[ExcelUpdateTools] Tool 'update_entity_stat' called with args: ${JSON.stringify(args)}`);
            const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
            const workbook = new ExcelJS.Workbook();

            try {
                // Read the existing workbook
                await workbook.xlsx.readFile(excelFilePath);
                const worksheet: Worksheet | undefined = workbook.getWorksheet(args.sheetName);

                if (!worksheet) {
                    return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' not found in '${excelFilePath}'.` }] };
                }

                const headerRowValues = worksheet.getRow(1).values as CellValue[];
                if (!headerRowValues || headerRowValues.length === 0) {
                    return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' has no header row or is empty.` }] };
                }
                // Make sure to handle cases where headerRowValues might have empty/null initial elements
                const headerRow = headerRowValues.filter(h => h !== null && typeof h !== 'undefined'); 

                const idColIndex = headerRow.findIndex(header => header && header.toString() === args.idColumnName);
                // actualIdColNumber will be 1-based for getCell, but for row.values (0-based array), we'd use idColIndex directly if headerRow was dense
                // Since headerRow could be sparse from worksheet.getRow(1).values, we need the actual column number from the worksheet perspective
                let actualIdColNumber = -1;
                worksheet.getRow(1).eachCell((cell, colNumber) => {
                    if (cell.value && cell.value.toString() === args.idColumnName) {
                        actualIdColNumber = colNumber; 
                    }
                });
                if (actualIdColNumber === -1) {
                     return { isError: true, content: [{ type: "text", text: `ID column '${args.idColumnName}' not found in sheet '${args.sheetName}'. Header: ${JSON.stringify(headerRowValues)}` }] };
                }

                let actualStatColNumber = -1;
                worksheet.getRow(1).eachCell((cell, colNumber) => {
                    if (cell.value && cell.value.toString() === args.statColumnName) {
                        actualStatColNumber = colNumber;
                    }
                });
                if (actualStatColNumber === -1) {
                    return { isError: true, content: [{ type: "text", text: `Stat column '${args.statColumnName}' not found in sheet '${args.sheetName}'. Header: ${JSON.stringify(headerRowValues)}` }] };
                }

                let targetRowNumber = -1;
                
                console.error(`[ExcelUpdateTools] Searching for entityId: '${args.entityId}' (Type: ${typeof args.entityId}) in column number ${actualIdColNumber} ('${args.idColumnName}')`);

                for (let i = 2; i <= worksheet.rowCount; i++) { // Start from row 2 (after header)
                    const row = worksheet.getRow(i);
                    // Accessing cell value via row.values (0-based) using actualIdColNumber (1-based)
                    const rowValues = row.values as CellValue[]; 
                    const cellValueRaw = rowValues[actualIdColNumber]; // row.values can be sparse and include an empty first element in some cases.
                                                                    // If actualIdColNumber is 1 (first column), then rowValues[1] is correct for such sparse arrays.
                                                                    // If row.values is truly 0-indexed dense array, then rowValues[actualIdColNumber - 1] is correct.
                                                                    // For safety, let's log it before using.

                    console.error(`[ExcelUpdateTools] Row ${i}, All RowRawValues: '${JSON.stringify(rowValues)}'`);
                    console.error(`[ExcelUpdateTools] Row ${i}, Attempting to get value from rowValues at index ${actualIdColNumber}. CellValueRaw: '${JSON.stringify(cellValueRaw)}' (Type: ${typeof cellValueRaw})`);

                    let cellValueAsString = '';
                    if (cellValueRaw !== null && typeof cellValueRaw !== 'undefined') {
                        cellValueAsString = cellValueRaw.toString().trim();
                    }
                    
                    const entityIdToCompare = args.entityId.trim();

                    console.error(`[ExcelUpdateTools] Comparing: '${cellValueAsString}' (Cell) with '${entityIdToCompare}' (Arg)`);

                    if (cellValueAsString === entityIdToCompare) {
                        targetRowNumber = i;
                        console.error(`[ExcelUpdateTools] Match found at row ${i}`);
                        break;
                    }
                }

                if (targetRowNumber === -1) {
                    return { isError: true, content: [{ type: "text", text: `Entity ID '${args.entityId}' not found in column '${args.idColumnName}' of sheet '${args.sheetName}'. Check logs for details.` }] };
                }

                const valueToSet = args.newValue === "" ? null : args.newValue;
                // Try to parse to float if it's a number, otherwise keep as string
                let finalValue: string | number | null = valueToSet;
                if (valueToSet !== null && valueToSet.trim() !== "") {
                    const parsedNum = parseFloat(valueToSet);
                    if (!isNaN(parsedNum)) {
                        finalValue = parsedNum;
                    }
                }

                worksheet.getCell(targetRowNumber, actualStatColNumber).value = finalValue;

                await workbook.xlsx.writeFile(excelFilePath);

                return { content: [{ type: "text", text: `Successfully updated stat '${args.statColumnName}' for entity '${args.entityId}' in sheet '${args.sheetName}' to '${args.newValue}'.` }] };

            } catch (error: any) {
                console.error(`[ExcelUpdateTools] Error in update_entity_stat for ID '${args.entityId}' in sheet '${args.sheetName}': ${error.message}`);
                return { isError: true, content: [{ type: "text", text: `Error processing update_entity_stat: ${error.message}` }] };
            }
        }
    );

    console.error("[ExcelUpdateTools] UPDATE Excel tools registration complete.");
}

module.exports = { registerUpdateExcelTools }; 