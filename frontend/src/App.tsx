import React, { useState, useCallback } from 'react';
import { Container, Typography, Box, Alert } from '@mui/material';
import axios from 'axios';

// Import components
import ParameterInput from './components/ParameterInput';
import MatrixInput from './components/MatrixInput';
import History from './components/History';
import SolutionPath from './components/SolutionPath';
import ResultDisplay from './components/ResultDisplay';

// Import types
import {
    TreasureHuntRequest,
    TreasureHuntResult,
    TreasureHuntResultWithPath,
    PaginatedResponse,
    PathStep
} from './types';

const formatFuelAsMath = (fuelUsed: number): string => {
    // Nếu là số nguyên (phần thập phân = 0) thì không hiển thị phần thập phân
    return Number.isInteger(fuelUsed) ? fuelUsed.toString() : fuelUsed.toFixed(5);
};

const App: React.FC = () => {
    // State for matrix dimensions and values
    const [n, setN] = useState<number>(3);
    const [m, setM] = useState<number>(3);
    const [p, setP] = useState<number>(3);
    const [matrix, setMatrix] = useState<string[][]>([]);

    // State for results and path
    const [result, setResult] = useState<number | null>(null);
    const [currentPath, setCurrentPath] = useState<PathStep[]>([]);
    const [selectedHistoryItem, setSelectedHistoryItem] = useState<TreasureHuntResultWithPath | null>(null);

    // UI state
    const [error, setError] = useState<string>('');
    const [loading, setLoading] = useState<boolean>(false);

    // History state
    const [history, setHistory] = useState<TreasureHuntResult[]>([]);
    const [historyPage, setHistoryPage] = useState<number>(1);
    const [itemsPerPage] = useState<number>(8);
    const [totalPages, setTotalPages] = useState<number>(0);
    const [totalCount, setTotalCount] = useState<number>(0);

    // Value change handlers
    const handleNChange = (value: number) => {
        setN(value);
    };

    const handleMChange = (value: number) => {
        setM(value);
    };

    const handlePChange = (value: number) => {
        setP(value);
    };

    // Create matrix with current dimensions (only when button is clicked)
    const createMatrix = () => {
        const newMatrix = Array(n).fill(null).map(() => Array(m).fill(''));
        setMatrix(newMatrix);
        setError('');
        setResult(null);
        setSelectedHistoryItem(null);
    };

    // Matrix change handler
    const handleMatrixChange = (row: number, col: number, value: string) => {
        const newMatrix = [...matrix];
        newMatrix[row][col] = value;
        setMatrix(newMatrix);
    };

    // Example loading handler
    const loadExample = (exampleNumber: number) => {
        switch (exampleNumber) {
            case 1:
                setN(3);
                setM(3);
                setP(3);
                setMatrix([
                    ['3', '2', '2'],
                    ['2', '2', '2'],
                    ['2', '2', '1']
                ]);
                break;
            case 2:
                setN(3);
                setM(4);
                setP(3);
                setMatrix([
                    ['2', '1', '1', '1'],
                    ['1', '1', '1', '1'],
                    ['2', '1', '1', '3']
                ]);
                break;
            case 3:
                setN(3);
                setM(4);
                setP(12);
                setMatrix([
                    ['1', '2', '3', '4'],
                    ['8', '7', '6', '5'],
                    ['9', '10', '11', '12']
                ]);
                break;
        }
    };

    // Random data generation handler
    const generateRandomData = async () => {
        setError('');
        setLoading(true);
        
        try {
            const response = await axios.get(`http://localhost:5001/api/generate-random-data?n=${n}&m=${m}&p=${p}`);
            const { n: newN, m: newM, p: newP, matrix: randomMatrix } = response.data;
            
            setN(newN);
            setM(newM);
            setP(newP);
            setMatrix(randomMatrix.map((row: number[]) => row.map(String)));
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to generate random data');
        } finally {
            setLoading(false);
        }
    };

    // File upload/export handlers
    const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) return;
        
        const reader = new FileReader();
        reader.onload = (e) => {
            try {
                if (!e.target?.result) return;
                const content = e.target.result as string;
                const lines = content.split('\n');
                const [newN, newM, newP] = lines[0].trim().split(/\s+/).map(Number);
                
                if (!newN || !newM || !newP || newN <= 0 || newM <= 0 || newP <= 0) {
                    setError('Invalid dimensions in file');
                    return;
                }
                
                const newMatrix = lines.slice(1, newN + 1).map(line => 
                    line.trim().split(/\s+/)
                );
                
                if (newMatrix.length !== newN || newMatrix.some(row => row.length !== newM)) {
                    setError('Invalid matrix dimensions in file');
                    return;
                }
                
                setN(newN);
                setM(newM);
                setP(newP);
                setMatrix(newMatrix);
                setError('');
            } catch (err) {
                setError('Invalid file format');
                console.error(err);
            }
        };
        reader.readAsText(file);
    };

    const handleExportMatrix = () => {
        if (matrix.length === 0) {
            setError('No matrix to export');
            return;
        }

        const content = [
            `${n} ${m} ${p}`,
            ...matrix.map(row => row.join(' '))
        ].join('\n');
        
        const blob = new Blob([content], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `treasure_hunt_matrix_${n}x${m}_p${p}.txt`;
        a.click();
        URL.revokeObjectURL(url);
    };

    // Matrix validation
    const validateMatrix = (): boolean => {
        if (!matrix || matrix.length === 0) {
            setError('Please create a matrix first');
            return false;
        }

        // Check if matrix exists and has correct dimensions
        if (matrix.length !== n || matrix.some(row => row.length !== m)) {
            setError('Matrix dimensions do not match current settings. Please create a new matrix.');
            return false;
        }

        // Check if all cells are filled
        for (let i = 0; i < n; i++) {
            for (let j = 0; j < m; j++) {
                if (!matrix[i][j] || isNaN(Number(matrix[i][j]))) {
                    setError('All matrix cells must be filled with valid numbers');
                    return false;
                }
            }
        }

        // Convert to numbers and validate range
        const numMatrix = matrix.map(row => row.map(cell => Number(cell)));
        const flatMatrix = numMatrix.flat();
        
        if (flatMatrix.some(val => val < 1 || val > p)) {
            setError(`All values must be between 1 and ${p}`);
            return false;
        }

        if (!flatMatrix.includes(p)) {
            setError(`Treasure chest with value ${p} must exist in the matrix`);
            return false;
        }

        return true;
    };

    // Load history
    const fetchHistory = useCallback(async () => {
        try {
            const response = await axios.get<PaginatedResponse<TreasureHuntResult>>(
                `http://localhost:5001/api/treasure-hunts?page=${historyPage}&pageSize=${itemsPerPage}`
            );
            setHistory(response.data.data);
            setTotalPages(response.data.totalPages);
            setTotalCount(response.data.totalCount);
        } catch (err) {
            console.error('Failed to fetch history:', err);
        }
    }, [historyPage, itemsPerPage]);

    // Solve handler
    const handleSolve = async () => {
        setError('');
        setResult(null);

        if (!validateMatrix()) {
            return;
        }

        setLoading(true);
        try {
            const numMatrix = matrix.map(row => row.map(cell => Number(cell)));
            const request: TreasureHuntRequest = {
                n,
                m,
                p,
                matrix: numMatrix
            };

            const response = await axios.post('http://localhost:5001/api/treasure-hunt/parallel', request);
            setResult(response.data.minFuel);
            setCurrentPath(response.data.path || []);
            setSelectedHistoryItem(null);
            setHistoryPage(1);
            await fetchHistory();
        } catch (err: any) {
            setError(err.response?.data?.message || 'An error occurred while solving the treasure hunt');
        } finally {
            setLoading(false);
        }
    };

    return (
        <Container maxWidth="lg" sx={{py: 2}}>
            <Typography variant="h3" component="h1" gutterBottom align="center" color="primary">
                🏴‍☠️ Treasure Hunt Solver
            </Typography>

            <Box sx={{display: 'flex', gap: 3, flexDirection: {xs: 'column', md: 'row'}}}>
                <Box sx={{flex: '0 0 50%'}}>
                    {/* Parameter Input */}
                    <ParameterInput 
                        n={n}
                        m={m}
                        p={p}
                        loading={loading}
                        onNChange={handleNChange}
                        onMChange={handleMChange}
                        onPChange={handlePChange}
                        onCreateMatrix={createMatrix}
                        onGenerateRandom={generateRandomData}
                        onLoadExample={loadExample}
                        onFileUpload={handleFileUpload}
                        onExportMatrix={handleExportMatrix}
                        onSolve={handleSolve}          
                    />

                    {/* Matrix Input */}
                    {matrix.length > 0 && (
                        <MatrixInput 
                            n={n}
                            m={m}
                            p={p}
                            matrix={matrix}
                            onMatrixChange={handleMatrixChange}
                        />
                    )}

                    {/* Error Display */}
                    {error && (
                        <Alert severity="error" sx={{mt: 2}}>
                            {error}
                        </Alert>
                    )}

                    {/* Result Display */}
                    <ResultDisplay 
                        result={result}
                        error=""
                        loading={loading}
                    />
                </Box>

                {/* History Panel */}
                <Box sx={{flex: 1}}>
                    <History 
                        history={history}
                        totalCount={totalCount}
                        totalPages={totalPages}
                        historyPage={historyPage}
                        onHistoryPageChange={setHistoryPage}
                        onHistoryItemClick={async (item: TreasureHuntResult) => {
                            try {
                                const response = await axios.get(`http://localhost:5001/api/treasure-hunt/${item.id}`);
                                const resultWithPath: TreasureHuntResultWithPath = response.data;
                                
                                setN(resultWithPath.n);
                                setM(resultWithPath.m);
                                setP(resultWithPath.p);
                                setMatrix(resultWithPath.matrix.map(row => row.map(String)));
                                setCurrentPath(resultWithPath.path);
                                setResult(resultWithPath.minFuel);
                                setSelectedHistoryItem(resultWithPath);
                                setError('');
                            } catch (err: any) {
                                setError('Failed to load treasure hunt details');
                            }
                        }}
                    />
                </Box>
            </Box>

            {/* Solution Path */}
            {(result !== null || selectedHistoryItem) && (
                <SolutionPath 
                    n={n}
                    m={m}
                    p={p}
                    matrix={matrix}
                    selectedHistoryMatrix={selectedHistoryItem?.matrix}
                    currentPath={currentPath}
                    selectedHistoryPath={selectedHistoryItem?.path}
                    result={result}
                    selectedHistoryItem={selectedHistoryItem}
                    formatFuelAsMath={formatFuelAsMath}
                />
            )}
        </Container>
    );
};

export default App;
