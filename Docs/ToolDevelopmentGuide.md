# MCP 도구 개발 가이드 (Node.js & TypeScript)

## 1. 문서 개요 (Overview)

*   **목적**: `sasha-unity` 프로젝트의 MCP (Meta Control Protocol) 도구 개발 가이드 (Node.js, TypeScript 환경 중심).
*   **주요 기술 스택**: Node.js, TypeScript, `@modelcontextprotocol/sdk`, `exceljs` (Excel 연동), `zod` (스키마 유효성 검사).
*   **예제 프로젝트**: `Tools/mcp-af-data-tool-server` (본 문서의 설명은 이 프로젝트 구조를 기반으로 함).

## 2. 필수 사전 지식 (Prerequisites)

*   **프로그래밍 언어**:
    *   JavaScript (ES6+): 비동기 처리 (Promise, async/await), 모듈 시스템 (CommonJS - `@server.ts` 등에서 `require` 사용).
    *   TypeScript: 기본 타입, 인터페이스, `any` 타입 사용에 대한 이해 (주의해서 사용).
*   **프레임워크 및 라이브러리**:
    *   Node.js: 기본 API, 이벤트 루프, `npm` 패키지 관리.
    *   `@modelcontextprotocol/sdk`: `McpServer`, `StdioServerTransport` 등의 핵심 클래스 사용법 (`@server.ts` 참조).
    *   `exceljs`: Excel 파일 (`.xlsx`) 읽기 및 데이터 처리 (`@excelGetTools.ts` 참조).
    *   `zod`: MCP 도구의 입력 인자 스키마 정의 및 유효성 검사 (`@excelGetTools.ts`의 `getSheetNamesSchema` 등 참조).
*   **데이터 형식**:
    *   JSON: MCP 도구의 반환 값 및 내부 데이터 표현.
    *   Excel (`.xlsx`): `AF_Data.xlsx`의 시트 구조 및 데이터 형식에 대한 이해 (예: `Frames`, `Parts` 시트).
*   **개발 도구**:
    *   Visual Studio Code (권장).
    *   Git: 프로젝트 `git-conventions.md` 숙지.
    *   `npm`: 의존성 관리 및 스크립트 실행 (`@package.json`의 `scripts` 참조).

## 3. 개발 환경 설정 (Development Environment Setup)

*   Node.js 및 `npm` 설치.
*   프로젝트 초기화 및 의존성 설치:
    *   `npm init -y` (새 프로젝트의 경우)
    *   `npm install @modelcontextprotocol/sdk exceljs zod`
    *   `npm install --save-dev typescript @types/node ts-node`
*   `tsconfig.json` 설정 (`@tsconfig.json` 파일 내용 참조):
    *   `target`: "ES2020"
    *   `module`: "CommonJS"
    *   `rootDir`: "./src"
    *   `outDir`: "./dist"
    *   `esModuleInterop`: true (CommonJS 모듈과의 호환성)
    *   `strict`: true (타입 검사 강화)
    *   `resolveJsonModule`: true (`package.json` 등을 직접 import 하기 위함)
*   `package.json` 설정 (`@package.json` 파일 내용 참조):
    *   `name`, `version`, `description` 등 프로젝트 정보.
    *   `main`: "dist/server.js" (컴파일된 진입점).
    *   `scripts`:
        *   `build`: "npx -p typescript tsc" (TypeScript 컴파일).
        *   `start`: "node dist/server.js" (컴파일된 서버 실행).
        *   `dev`: "ts-node src/server.ts" (개발 중 실행).
    *   `dependencies`, `devDependencies`.

## 4. MCP 도구 개발 가이드 (Node.js & TypeScript)

*   **프로젝트 구조 (`mcp-af-data-tool-server` 예시)**:
    *   `src/`: TypeScript 소스 파일 디렉토리.
        *   `server.ts`: MCP 서버 초기화, 설정, 도구 등록 로직 포함 (메인 진입점).
        *   `excelTools.ts`: 여러 Excel 관련 도구 그룹을 등록하는 중계 모듈.
        *   `excelGetTools.ts`: 실제 "GET" 계열 Excel 도구들의 로직 구현 (스키마 정의, 핸들러 함수).
        *   (필요시 `excelSetTools.ts` 등 기능별 모듈 추가)
    *   `dist/`: `tsc`로 컴파일된 JavaScript 파일들이 위치하는 디렉토리.
    *   기타 설정 파일: `package.json`, `package-lock.json`, `tsconfig.json`.
*   **핵심 개념 및 구현 (첨부된 파일 기반 설명)**:
    *   **서버 설정 (`@server.ts`)**:
        *   `McpServer`, `StdioServerTransport` import (`@modelcontextprotocol/sdk/server/...`).
        *   `packageJson` import (`resolveJsonModule: true` 필요).
        *   `McpServer` 인스턴스 생성 (name, version 등 `package.json` 값 활용).
        *   `registerExcelTools(server)` 호출로 도구 등록 (`@excelTools.ts`의 함수).
        *   `StdioServerTransport`를 사용해 서버 연결 및 표준 입출력으로 통신.
        *   `console.error`를 통한 로깅 (MCP 서버는 표준 출력을 통신 채널로 사용하므로, 로그는 표준 에러로 출력).
    *   **도구 등록 및 모듈화 (`@excelTools.ts`, `@excelGetTools.ts`)**:
        *   관련 도구들을 별도의 파일/모듈로 분리 (예: `excelGetTools.ts`).
        *   `@excelTools.ts`에서 각 모듈의 등록 함수 (`registerGetExcelTools`)를 호출하여 서버에 통합.
    *   **개별 도구 정의 (`@excelGetTools.ts`)**:
        *   `server.tool("tool_name", schema, async_handler_function)` 형식으로 도구 정의.
        *   **입력 스키마 (Zod 사용)**:
            *   `z.object({...})`로 인자 객체 스키마 정의.
            *   `z.string().optional().describe("...")` 등으로 각 인자의 타입, 필수 여부, 설명 정의.
            *   예시: `getSheetNamesSchema`, `readSheetDataSchema`, `getEntityDetailsSchema`.
        *   **핸들러 함수 (비동기)**:
            *   `async (args: { arg1?: string, ... }) => { ... }` 형태. `args` 타입은 Zod 스키마로부터 추론 가능.
            *   `args.filePath || DEFAULT_EXCEL_FILE_PATH`와 같이 기본값 처리.
            *   `exceljs`를 사용한 Excel 파일 처리:
                *   `workbook.xlsx.readFile(excelFilePath)`
                *   `workbook.getWorksheet(sheetName)`
                *   `worksheet.eachRow(...)`, `worksheet.getRow(1).values` (헤더 추출)
            *   **반환 값 형식**:
                *   성공: `{ content: [{ type: "text", text: JSON.stringify(result) }] }`
                *   실패: `{ isError: true, content: [{ type: "text", text: "Error message" }] }`
            *   `path.join(__dirname, relativePath)`를 사용한 파일 경로 계산 (`DEFAULT_EXCEL_FILE_PATH`).
                *   주의: `__dirname`은 컴파일된 JavaScript 파일(`dist/`)의 위치를 기준으로 하므로, 원본 `.xlsx` 파일과의 상대 경로 설정에 유의.
        *   **로깅**: `console.error`를 사용해 디버깅 및 실행 흐름 추적.

## 5. 빌드 및 실행

*   **빌드**: `npm run build` (또는 `npx tsc`). `dist` 폴더에 JavaScript 파일 생성.
*   **실행**: `npm start` (또는 `node dist/server.js`).
*   **개발 모드 실행**: `npm run dev` (또_는 `ts-node src/server.ts`). `ts-node`를 사용해 컴파일 과정 없이 TypeScript 직접 실행.

## 6. 자주 발생하는 문제 및 해결 방법 (Troubleshooting & FAQ - Node.js/TypeScript)

*   **모듈 관련 오류 (`Cannot find module '...'`)**:
    *   `npm install` 실행 여부 확인.
    *   `package.json`의 `dependencies` 또는 `devDependencies`에 해당 모듈이 올바르게 명시되어 있는지 확인.
    *   `tsconfig.json`의 `moduleResolution` 옵션 및 경로 관련 설정 확인.
    *   CommonJS 모듈(`require`)과 ES 모듈(`import`) 혼용 시 `esModuleInterop: true` 설정 확인.
*   **TypeScript 컴파일 오류**:
    *   `tsc` 실행 시 출력되는 오류 메시지 꼼꼼히 확인.
    *   타입 불일치: 변수, 함수 인자, 반환 값 등의 타입 선언 확인. `any` 타입 사용 최소화.
    *   `tsconfig.json`의 `strict` 관련 옵션 활성화 시 더 많은 타입 오류가 발생할 수 있으나, 코드 안정성 향상에 도움됨.
*   **경로 문제 (특히 `AF_Data.xlsx` 같은 외부 파일 접근)**:
    *   `path.join(__dirname, ...)` 사용 시 `__dirname`이 개발 시점(`src/`)과 실행 시점(`dist/`)에서 다를 수 있음을 인지.
    *   상대 경로는 스크립트 실행 위치에 따라 달라지므로, 절대 경로를 사용하거나 프로젝트 루트 기준 상대 경로를 명확히 설정.
    *   `DEFAULT_EXCEL_FILE_PATH`와 같이 경로를 상수로 관리.
*   **`exceljs` 사용 오류**:
    *   파일 경로가 올바르지 않아 파일을 찾지 못하는 경우.
    *   시트 이름이 잘못되어 시트를 찾지 못하는 경우.
    *   Excel 파일 형식 문제 또는 손상.
*   **`zod` 스키마 유효성 검사 실패**:
    *   MCP 클라이언트(호출하는 측)에서 도구 호출 시 전달하는 인자가 정의된 스키마와 일치하지 않는 경우. 