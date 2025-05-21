const { z } = require("zod");
const ExcelJS = require('exceljs');
const path = require('path');
import type { CellValue, Row, Worksheet, CellRichTextValue, Font, RichText } from 'exceljs';

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

// Schema for the get_grouped_stats_summary tool
const getGroupedStatsSummarySchema = {
    sheetName: z.string().describe("The name of the sheet to analyze (e.g., 'Parts', 'Frames')."),
    groupByColumn: z.string().describe("The name of the column to group by (e.g., 'FrameType', 'WeaponTier')."),
    statColumnsToAnalyze: z.array(z.string()).min(1).describe("An array of stat column names to analyze (e.g., ['Stat_HP', 'Stat_AttackPower'])."),
    filePath: z.string().optional().describe("Optional path to the Excel file. Defaults to AF_Data.xlsx relative to project root.")
};

// Schema for get_assembly_details
const getAssemblyDetailsSchema = {
    assemblyId: z.string().describe("The ID of the assembly to get details for (e.g., 'Cobalt_Vanguard')."),
    includeComponentDetails: z.boolean().optional().default(false).describe("If true, includes detailed information for each component (frame, parts, pilot). Defaults to false, returning only total stats and abilities."),
    filePath: z.string().optional().describe("Optional path to the Excel file. Defaults to AF_Data.xlsx relative to project root.")
};

// Interface for stat collection, making values optional for final deletion
interface StatCollection {
    values?: number[]; 
    count: number;
    sum: number;
    average: number | null;
    min: number | null;
    max: number | null;
}

/**
 * Helper function to find an entity by ID in a given worksheet.
 * @param worksheet The ExcelJS Worksheet object.
 * @param entityId The ID of the entity to find.
 * @param idColumnName The name of the column containing the ID.
 * @param headerRowValues Array of header cell values.
 * @returns The entity data as a Record<string, any> or null if not found.
 */
function _findEntityById(worksheet: Worksheet, entityId: string, idColumnName: string, headerRowValues: CellValue[]): Record<string, any> | null {
    if (!headerRowValues || headerRowValues.length === 0) {
        console.error("[_findEntityById] Error: Header row is empty or undefined.");
        return null; // Or throw error
    }

    // Find the 0-based index of the ID column from the headerRowValues array
    // Note: worksheet.getRow(1).values can be sparse, so we directly use the passed headerRowValues
    const idColIndex = headerRowValues.findIndex(header => header && header.toString().trim() === idColumnName.trim());

    if (idColIndex === -1) {
        console.error(`[_findEntityById] Error: ID column '${idColumnName}' not found in headers.`);
        return null; // Or throw error
    }

    let foundEntityData: Record<string, any> | null = null;

    // Iterate from row 2 (assuming row 1 is header)
    for (let i = 2; i <= worksheet.rowCount; i++) {
        const row = worksheet.getRow(i);
        const rowValues = row.values as CellValue[]; // Can be sparse, first element might be null or match worksheet structure
        
        // Adjust index for sparse arrays if the first element of row.values is null and others are shifted
        // ExcelJS row.values can sometimes be [null, val1, val2, ...]
        // So, if headerRowValues[idColIndex] corresponds to the Nth logical column,
        // we need to access rowValues[idColIndex] if it's dense, or rowValues[N] if sparse starting with null.
        // The key is that idColIndex is derived from headerRowValues which should match the structure of row.values.
        const cellValue = rowValues[idColIndex];

        if (cellValue && cellValue.toString().trim() === entityId.trim()) {
            foundEntityData = {};
            headerRowValues.forEach((header, index) => {
                if (header) {
                    // Use the same index for rowValues as for headerRowValues
                    (foundEntityData as Record<string,any>)[header.toString().trim()] = rowValues[index];
                }
            });
            break; // Found the entity, no need to iterate further
        }
    }
    return foundEntityData;
}

/**
 * Registers GET Excel-related tools with the MCP server.
 * @param {any} server MCP Server instance
 */
function registerGetExcelTools(server: any) {
    console.error("[ExcelGetTools] Registering GET Excel tools...");

    server.tool(
        "get_sheet_names",
        getSheetNamesSchema,
        async (args: { filePath?: string }) => {
            console.error(`[ExcelGetTools] Tool 'get_sheet_names' called with args: ${JSON.stringify(args)}`);
            const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
            try {
                const workbook = new ExcelJS.Workbook();
                await workbook.xlsx.readFile(excelFilePath);
                const sheetNames = workbook.worksheets.map((ws: any) => ws.name);
                return { content: [{ type: "text", text: JSON.stringify(sheetNames) }] };
            } catch (error: any) {
                console.error(`[ExcelGetTools] Error in get_sheet_names for file '${excelFilePath}': ${error.message}`);
                return { isError: true, content: [{ type: "text", text: `Error getting sheet names from '${excelFilePath}': ${error.message}` }] };
            }
        }
    );

    server.tool(
        "read_sheet_data",
        readSheetDataSchema,
        async (args: { sheetName?: string, filePath?: string }) => {
            console.error(`[ExcelGetTools] Tool 'read_sheet_data' called with args: ${JSON.stringify(args)}`);
            const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
            try {
                const workbook = new ExcelJS.Workbook();
                await workbook.xlsx.readFile(excelFilePath);
                const worksheet = workbook.getWorksheet(args.sheetName!);
                if (!worksheet) {
                    return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' not found in '${excelFilePath}'.` }] };
                }
                
                const jsonData: any[] = [];
                const headerRow = worksheet.getRow(1).values as any[];
                worksheet.eachRow((row: any, rowNumber: number) => {
                    if (rowNumber > 1) { 
                        let rowData: Record<string, any> = {};
                        row.values.forEach((value: any, index: number) => { 
                            const headerName = headerRow[index] ? headerRow[index].toString() : `column${index}`;
                            rowData[headerName] = value;
                        });
                        jsonData.push(rowData);
                    }
                });
                return { content: [{ type: "text", text: JSON.stringify(jsonData, null, 2) }] };
            } catch (error: any) {
                console.error(`[ExcelGetTools] Error in read_sheet_data for sheet '${args.sheetName}' in file '${excelFilePath}': ${error.message}`);
                return { isError: true, content: [{ type: "text", text: `Error reading sheet '${args.sheetName}' from '${excelFilePath}': ${error.message}` }] };
            }
        }
    );

    server.tool(
        "get_entity_details",
        getEntityDetailsSchema,
        async (args: { sheetName: string, entityId: string, idColumnName: string, filePath?: string }) => {
            console.error(`[ExcelGetTools] Tool 'get_entity_details' called with args: ${JSON.stringify(args)}`);
            const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
            try {
                const workbook = new ExcelJS.Workbook();
                await workbook.xlsx.readFile(excelFilePath);
                const worksheet = workbook.getWorksheet(args.sheetName);
                if (!worksheet) {
                    return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' not found in '${excelFilePath}'.` }] };
                }

                // worksheet.getRow(1).values can be sparse, like [null, 'Header1', 'Header2', ...]
                // It's important to get it once and pass it to the helper.
                const headerRowCellValues = worksheet.getRow(1).values as CellValue[];
                if (!headerRowCellValues || headerRowCellValues.filter(h => h !== null && typeof h !== 'undefined').length === 0) {
                    return { isError: true, content: [{ type: "text", text: `Sheet '${args.sheetName}' has no header row or is empty.` }] };
                }

                // Call the helper function
                const foundEntityData = _findEntityById(worksheet, args.entityId, args.idColumnName, headerRowCellValues);

                if (foundEntityData) {
                    return { content: [{ type: "text", text: JSON.stringify(foundEntityData, null, 2) }] };
                } else {
                    // Check if the ID column itself was not found by _findEntityById (it returns null in that case too)
                    const idColIndex = headerRowCellValues.findIndex(header => header && header.toString().trim() === args.idColumnName.trim());
                    if (idColIndex === -1) {
                         return { isError: true, content: [{ type: "text", text: `ID column '${args.idColumnName}' not found in sheet '${args.sheetName}'.` }] };
                    }
                    return { content: [{ type: "text", text: `Entity with ID '${args.entityId}' not found in column '${args.idColumnName}' of sheet '${args.sheetName}'.` }] };
                }

            } catch (error: any) {
                console.error(`[ExcelGetTools] Error in get_entity_details for ID '${args.entityId}' in sheet '${args.sheetName}': ${error.message}`);
                return { isError: true, content: [{ type: "text", text: `Error processing get_entity_details: ${error.message}` }] };
            }
        }
    );

    // New tool: get_grouped_stats_summary
    server.tool(
        "get_grouped_stats_summary",
        getGroupedStatsSummarySchema,
        async (args: {
            sheetName: string,
            groupByColumn: string,
            statColumnsToAnalyze: string[],
            filePath?: string
        }) => {
            console.error(`[ExcelGetTools] Tool 'get_grouped_stats_summary' called with args: ${JSON.stringify(args)}`);
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
                
                let groupByColNumber = -1;
                headerRow.eachCell((cell, colNumber) => {
                    if (cell.value && cell.value.toString().trim() === args.groupByColumn.trim()) {
                        groupByColNumber = colNumber;
                    }
                });

                if (groupByColNumber === -1) {
                    return { isError: true, content: [{ type: "text", text: `Group by column '${args.groupByColumn}' not found in sheet '${args.sheetName}'.` }] };
                }

                const statColNumbers: { [key: string]: number } = {};
                let allStatColsFound = true;
                for (const statColName of args.statColumnsToAnalyze) {
                    let found = false;
                    headerRow.eachCell((cell, colNumber) => {
                        if (cell.value && cell.value.toString().trim() === statColName.trim()) {
                            statColNumbers[statColName] = colNumber;
                            found = true;
                        }
                    });
                    if (!found) {
                        allStatColsFound = false;
                        console.error(`[ExcelGetTools] Stat column '${statColName}' not found in sheet '${args.sheetName}'.`);
                    }
                }

                if (!allStatColsFound || Object.keys(statColNumbers).length !== args.statColumnsToAnalyze.length) {
                     return { isError: true, content: [{ type: "text", text: `One or more stat columns not found in sheet '${args.sheetName}'. Check logs.` }] };
                }

                const groupedStats: { 
                    [groupName: string]: { 
                        [statName: string]: StatCollection
                    } 
                } = {};

                for (let i = 2; i <= worksheet.rowCount; i++) {
                    const currentRow = worksheet.getRow(i);
                    const groupKeyValueCell = currentRow.getCell(groupByColNumber).value;
                    const groupKey = groupKeyValueCell ? groupKeyValueCell.toString().trim() : "N/A";

                    if (!groupedStats[groupKey]) {
                        groupedStats[groupKey] = {};
                        args.statColumnsToAnalyze.forEach(statName => {
                            groupedStats[groupKey][statName] = {
                                values: [],
                                count: 0,
                                sum: 0,
                                average: null,
                                min: null,
                                max: null
                            };
                        });
                    }

                    args.statColumnsToAnalyze.forEach(statName => {
                        const statColNum = statColNumbers[statName];
                        const cellValue = currentRow.getCell(statColNum).value;
                        
                        let numericValue: number | null = null;
                        if (cellValue !== null && typeof cellValue !== 'undefined') {
                            if (typeof cellValue === 'object' && (cellValue as CellRichTextValue).richText) {
                                const richText = (cellValue as CellRichTextValue).richText;
                                let textContent = "";
                                richText.forEach(rt => textContent += rt.text);
                                numericValue = parseFloat(textContent.trim());
                            } else {
                                numericValue = parseFloat(cellValue.toString().trim());
                            }
                        }

                        if (numericValue !== null && !isNaN(numericValue) && groupedStats[groupKey][statName].values) {
                            (groupedStats[groupKey][statName].values as number[]).push(numericValue);
                        } else if (numericValue === null || isNaN(numericValue)) {
                            // console.error(`[ExcelGetTools] Row ${i}, Column '${statName}': Value '${cellValue}' is not a valid number. Skipping.`);
                        }
                    });
                }
                
                // Calculate statistics
                for (const groupName in groupedStats) {
                    for (const statName in groupedStats[groupName]) {
                        const statData = groupedStats[groupName][statName];
                        if (statData.values && statData.values.length > 0) {
                            statData.count = statData.values.length;
                            statData.sum = statData.values.reduce((acc, val) => acc + val, 0);
                            statData.average = statData.sum / statData.count;
                            statData.min = Math.min(...statData.values);
                            statData.max = Math.max(...statData.values);
                        }
                        delete statData.values; // Now this should be fine as values is optional
                    }
                }

                return { content: [{ type: "text", text: JSON.stringify(groupedStats, null, 2) }] };

            } catch (error: any) {
                console.error(`[ExcelGetTools] Error in get_grouped_stats_summary: ${error.message}`);
                console.error(error.stack);
                return { isError: true, content: [{ type: "text", text: `Error processing get_grouped_stats_summary: ${error.message}` }] };
            }
        }
    );

    // New tool: get_assembly_details
    server.tool(
        "get_assembly_details",
        getAssemblyDetailsSchema,
        async (args: { assemblyId: string, includeComponentDetails?: boolean, filePath?: string }) => {
            console.error(`[ExcelGetTools] Tool 'get_assembly_details' called with args: ${JSON.stringify(args)}`);
            const excelFilePath = args.filePath || DEFAULT_EXCEL_FILE_PATH;
            const includeDetails = args.includeComponentDetails === undefined ? false : args.includeComponentDetails; // Explicitly handle default

            try {
                const workbook = new ExcelJS.Workbook();
                await workbook.xlsx.readFile(excelFilePath);

                const assembliesSheet = workbook.getWorksheet('AF_Assemblies');
                if (!assembliesSheet) return { isError: true, content: [{ type: "text", text: "Sheet 'AF_Assemblies' not found." }] };
                
                const assembliesHeader = assembliesSheet.getRow(1).values as CellValue[];
                const assemblyData = _findEntityById(assembliesSheet, args.assemblyId, 'AssemblyID', assembliesHeader);

                if (!assemblyData) return { isError: true, content: [{ type: "text", text: `Assembly with ID '${args.assemblyId}' not found.` }] };

                const assemblyName = assemblyData['AssemblyName'] || args.assemblyId;
                let result: any = { assemblyName };

                const componentPromises = [];
                const componentDetails: any = {};

                const processEntity = async (entityId: string | null, sheetName: string, idColName: string, entityType: string) => {
                    console.error(`[ExcelGetTools-Debug] processEntity called with: entityId='${entityId}', sheetName='${sheetName}', idColName='${idColName}', entityType='${entityType}'`);
                    if (!entityId || typeof entityId !== 'string' || entityId.trim() === '') {
                        // console.warn(`[ExcelGetTools] Skipping ${entityType} due to missing or invalid ID.`);
                        console.error(`[ExcelGetTools-Debug] processEntity returning null due to invalid entityId: '${entityId}'`);
                        return null;
                    }
                    const sheet = workbook.getWorksheet(sheetName);
                    if (!sheet) {
                        console.error(`[ExcelGetTools] Sheet '${sheetName}' not found for ${entityType} ID ${entityId}.`);
                        console.error(`[ExcelGetTools-Debug] processEntity returning error object: Sheet '${sheetName}' not found.`);
                        return { id: entityId, error: `Sheet '${sheetName}' not found.` };
                    }
                    const header = sheet.getRow(1).values as CellValue[];
                    const data = _findEntityById(sheet, entityId, idColName, header);
                    console.error(`[ExcelGetTools-Debug] _findEntityById in processEntity for '${entityId}' in '${sheetName}' returned: ${JSON.stringify(data)}`);
                    if (!data) {
                        console.warn(`[ExcelGetTools] ${entityType} with ID '${entityId}' not found in sheet '${sheetName}'.`);
                        console.error(`[ExcelGetTools-Debug] processEntity returning error object: ${entityType} not found.`);
                        return { id: entityId, error: `${entityType} not found.` };
                    }
                    // console.log(\`[ExcelGetTools] Fetched ${entityType}: \`, data);
                    console.error(`[ExcelGetTools-Debug] processEntity successfully fetched data for '${entityId}'.`);
                    return data;
                };

                // Frame
                componentPromises.push(processEntity(assemblyData['FrameID'] as string, 'Frames', 'FrameID', 'frame').then(data => {
                    if (data && !data.error) componentDetails.frame = data;
                    else if (data && data.error) componentDetails.frame = { id: assemblyData['FrameID'], error: data.error };
                }));

                // Pilot
                componentPromises.push(processEntity(assemblyData['PilotID'] as string, 'Pilots', 'PilotID', 'pilot').then(data => {
                    if (data && !data.error) componentDetails.pilot = data;
                    else if (data && data.error) componentDetails.pilot = { id: assemblyData['PilotID'], error: data.error };
                }));
                
                // Parts
                const partPromises: Promise<any>[] = [];
                // AF_Assemblies 시트의 실제 컬럼 이름으로 수정
                const partIdColumns = { 
                    head: 'HeadPartID', 
                    body: 'BodyPartID', 
                    // Arms는 Left/Right 구분 필요. 아래에서 별도 처리
                    legs: 'LegsPartID', 
                    // Booster, FCS, Generator는 현재 Cobalt_Vanguard에 없으므로, 
                    // 다른 어셈블리에서 사용된다면 해당 컬럼명 추가 필요.
                    // 예: booster: 'BoosterPartID', fcs: 'FCS_PartID', generator: 'GeneratorPartID'
                };
                // Weapon ID 컬럼도 실제 이름으로 수정
                const weaponIdColumns = {
                    weapon1: 'Weapon1ID', // 보통 오른팔 무기
                    weapon2: 'Weapon2ID', // 보통 왼팔 무기
                    // 필요하다면 R_Back, L_Back 등 추가
                };
                
                componentDetails.parts = {}; // Initialize parts object

                // 일반 파츠 처리
                for (const partType in partIdColumns) {
                    const colName = partIdColumns[partType as keyof typeof partIdColumns];
                    const partId = assemblyData[colName] as string;
                    if (partId && partId.trim() !== '') {
                        partPromises.push(processEntity(partId, 'Parts', 'PartID', `part (${partType})`).then(data => {
                            if (data && !data.error) componentDetails.parts[partType] = data;
                            else if (data && data.error) componentDetails.parts[partType] = { id: partId, error: data.error };
                        }));
                    }
                }

                // 팔 파츠 처리 (좌/우 구분)
                const leftArmId = assemblyData['LeftArmPartID'] as string;
                if (leftArmId && leftArmId.trim() !== '') {
                    partPromises.push(processEntity(leftArmId, 'Parts', 'PartID', 'part (leftarm)').then(data => {
                        if (data && !data.error) componentDetails.parts['leftarm'] = data;
                        else if (data && data.error) componentDetails.parts['leftarm'] = { id: leftArmId, error: data.error };
                    }));
                }
                const rightArmId = assemblyData['RightArmPartID'] as string;
                if (rightArmId && rightArmId.trim() !== '') {
                    partPromises.push(processEntity(rightArmId, 'Parts', 'PartID', 'part (rightarm)').then(data => {
                        if (data && !data.error) componentDetails.parts['rightarm'] = data;
                        else if (data && data.error) componentDetails.parts['rightarm'] = { id: rightArmId, error: data.error };
                    }));
                }
                
                // 무기 처리
                for (const weaponSlot in weaponIdColumns) {
                    const colName = weaponIdColumns[weaponSlot as keyof typeof weaponIdColumns];
                    const weaponId = assemblyData[colName] as string;
                    if (weaponId && weaponId.trim() !== '') {
                        partPromises.push(processEntity(weaponId, 'Weapons', 'WeaponID', `weapon (${weaponSlot})`).then(data => {
                            // 무기는 'parts' 하위가 아닌 별도 카테고리 'weapons'로 관리하거나, 
                            // 'parts'에 넣되 slot 이름으로 구분 (현재는 weaponSlot으로 parts에 넣고 있음)
                            if (data && !data.error) componentDetails.parts[weaponSlot] = data; 
                            else if (data && data.error) componentDetails.parts[weaponSlot] = { id: weaponId, error: data.error };
                        }));
                    }
                }

                await Promise.all([...componentPromises, ...partPromises]);
                
                // Calculate Total Stats and Abilities
                const totalStats: Record<string, number> = {};
                const allAbilities = new Set<string>();

                const accumulateStats = (component: any) => {
                    if (!component || component.error) return;
                    for (const key in component) {
                        if (key.startsWith('Stat_')) {
                            const statValue = component[key];
                            let numericValue: number | null = null;

                            if (statValue !== null && typeof statValue !== 'undefined') {
                                if (typeof statValue === 'number') {
                                    numericValue = statValue;
                                } else if (typeof statValue === 'string') {
                                    const parsed = parseFloat(statValue);
                                    if (!isNaN(parsed)) {
                                        numericValue = parsed;
                                    } else {
                                        // console.warn(`[ExcelGetTools] Stat '${key}' value '${statValue}' is a string but not a valid number. Skipping.`);
                                    }
                                } else if (typeof statValue === 'object' && (statValue as CellRichTextValue).richText) {
                                    // Handle RichText case if necessary, though stats are usually not RichText
                                    let textContent = "";
                                    (statValue as CellRichTextValue).richText.forEach(rt => textContent += rt.text);
                                    const parsed = parseFloat(textContent.trim());
                                    if (!isNaN(parsed)) {
                                        numericValue = parsed;
                                    } else {
                                        // console.warn(`[ExcelGetTools] Stat '${key}' RichText value '${textContent}' is not a valid number. Skipping.`);
                                    }
                                }
                            }

                            if (numericValue !== null) {
                                totalStats[key] = (totalStats[key] || 0) + numericValue;
                            }
                        } else if (key === 'Abilities') {
                            const abilitiesValue = component[key];
                            if (typeof abilitiesValue === 'string' && abilitiesValue.trim() !== '') {
                                abilitiesValue.split(',').forEach((ability: string) => allAbilities.add(ability.trim()));
                            }
                        } else if (key.startsWith('Ability_')) { // Handle Ability_XXX format
                            const abilityValue = component[key];
                            if (typeof abilityValue === 'string' && abilityValue.trim() !== '') {
                                allAbilities.add(abilityValue.trim());
                            }
                        }
                    }
                };

                accumulateStats(componentDetails.frame);
                accumulateStats(componentDetails.pilot);

                // Define a more specific type for what 'part' could be
                type ProcessedPart = Record<string, any> | { id: string, error: string };

                Object.values(componentDetails.parts).forEach((partValue) => {
                    const part = partValue as ProcessedPart; // Cast to the more specific type
                    // Check if 'error' property does NOT exist or is falsy. Also check if 'id' exists for the error case.
                    if (part && !(part as { error: string }).error) { 
                        accumulateStats(part);
                    } else if (part && (part as { id: string; error: string }).id && (part as { id: string; error: string }).error) {
                        console.warn(`[ExcelGetTools] Skipping stats accumulation for part ID '${(part as { id: string; error: string }).id}' due to error: ${(part as { id: string; error: string }).error}`);
                    }
                });
                
                result.totalStats = totalStats;
                result.allUniqueAbilities = Array.from(allAbilities);

                if (includeDetails) {
                    result.components = componentDetails;
                }

                return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };

            } catch (error: any) {
                console.error(`[ExcelGetTools] Error in get_assembly_details: ${error.message}`);
                console.error(error.stack);
                return { isError: true, content: [{ type: "text", text: `Error processing get_assembly_details: ${error.message}` }] };
            }
        }
    );

    console.error("[ExcelGetTools] GET Excel tools registration complete.");
}

module.exports = { registerGetExcelTools }; 