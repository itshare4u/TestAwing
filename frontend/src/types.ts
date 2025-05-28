export interface TreasureHuntRequest {
    n: number;
    m: number;
    p: number;
    matrix: number[][];
}

export interface PathStep {
    chestNumber: number;
    row: number;
    col: number;
    fuelUsed: number;
    cumulativeFuel: number;
}

export interface TreasureHuntResult {
    id: number;
    n: number;
    m: number;
    p: number;
    matrixJson: string;
    minFuel: number;
    createdAt: string;
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
