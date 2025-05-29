export interface TreasureHuntRequest {
    n: number;
    m: number;
    p: number;
    matrix: number[][];
}

export enum SolveStatus {
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4
}

export interface PathStep {
    chestNumber: number;
    row: number;
    col: number;
    fuelUsed: number;
    cumulativeFuel: number;
}

export interface TreasureHuntResponse {
    minFuel: number;
    id: number;
    path: PathStep[];
}

export interface TreasureHuntResult {
    id: number;
    n: number;
    m: number;
    p: number;
    matrixJson: string;
    minFuel: number;
    createdAt: string;
    status: SolveStatus;
    startedAt?: string;
    completedAt?: string;
    errorMessage?: string;
}

export interface AsyncSolveRequest {
    treasureHuntRequest: TreasureHuntRequest;
}

export interface AsyncSolveResponse {
    solveId: number;
    status: SolveStatus;
    message: string;
}

export interface SolveStatusResponse {
    solveId: number;
    status: SolveStatus;
    result?: TreasureHuntResponse;
    errorMessage?: string;
    createdAt: string;
    startedAt?: string;
    completedAt?: string;
}

export interface TreasureHuntResultWithPath {
    id: number;
    n: number;
    m: number;
    p: number;
    matrix: number[][];
    path: PathStep[];
    minFuel: number;
    createdAt: string;
}

export interface PaginatedResponse<T> {
    data: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}
