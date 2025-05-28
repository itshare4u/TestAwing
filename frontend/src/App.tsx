import React, {useState, useEffect, useCallback} from 'react';
import {
    Container,
    Typography,
    Paper,
    TextField,
    Button,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Alert,
    Box,
    Card,
    CardContent,
    Divider,
    Pagination
} from '@mui/material';
import { Shuffle } from '@mui/icons-material';
import {styled} from '@mui/material/styles';
import axios from 'axios';

const StyledPaper = styled(Paper)(({theme}) => ({
    padding: theme.spacing(3),
    marginBottom: theme.spacing(2),
}));

interface TreasureHuntRequest {
    n: number;
    m: number;
    p: number;
    matrix: number[][];
}

interface PathStep {
    chestNumber: number;
    row: number;
    col: number;
    fuelUsed: number;
    cumulativeFuel: number;
}

interface TreasureHuntResult {
    id: number;
    n: number;
    m: number;
    p: number;
    matrixJson: string;
    minFuel: number;
    createdAt: string;
}

interface TreasureHuntResultWithPath {
    id: number;
    n: number;
    m: number;
    p: number;
    matrix: number[][];
    path: PathStep[];
    minFuel: number;
    createdAt: string;
}

interface PaginatedResponse<T> {
    data: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}

const App: React.FC = () => {
    const [n, setN] = useState<number>(3);
    const [m, setM] = useState<number>(3);
    const [p, setP] = useState<number>(3);
    const [matrix, setMatrix] = useState<string[][]>([]);
    const [result, setResult] = useState<number | null>(null);
    const [currentPath, setCurrentPath] = useState<PathStep[]>([]);
    const [selectedHistoryItem, setSelectedHistoryItem] = useState<TreasureHuntResultWithPath | null>(null);
    const [error, setError] = useState<string>('');
    const [loading, setLoading] = useState<boolean>(false);
    const [history, setHistory] = useState<TreasureHuntResult[]>([]);
    const [historyPage, setHistoryPage] = useState<number>(1);
    const [itemsPerPage] = useState<number>(8);
    const [totalPages, setTotalPages] = useState<number>(0);
    const [totalCount, setTotalCount] = useState<number>(0);

    // Initialize matrix when dimensions change
    useEffect(() => {
        const newMatrix = Array(n).fill(null).map(() => Array(m).fill(''));
        setMatrix(newMatrix);
    }, [n, m]);

    // Load history on component mount and when page changes
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

    useEffect(() => {
        fetchHistory();
    }, [fetchHistory]);

    const validateMatrix = (): boolean => {
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

        // Check if all values are in range [1, p]
        if (flatMatrix.some(val => val < 1 || val > p)) {
            setError(`All values must be between 1 and ${p}`);
            return false;
        }

        // Check if treasure chest (p) exists
        if (!flatMatrix.includes(p)) {
            setError(`Treasure chest with value ${p} must exist in the matrix`);
            return false;
        }

        return true;
    };

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

            const response = await axios.post('http://localhost:5001/api/treasure-hunt', request);
            setResult(response.data.minFuel);
            setCurrentPath(response.data.path || []);
            setSelectedHistoryItem(null); // Clear any selected history item
            setHistoryPage(1); // Reset to first page when new data is added
            fetchHistory(); // Refresh history
        } catch (err: any) {
            setError(err.response?.data?.message || 'An error occurred while solving the treasure hunt');
        } finally {
            setLoading(false);
        }
    };

    const handleMatrixChange = (row: number, col: number, value: string) => {
        const newMatrix = [...matrix];
        newMatrix[row][col] = value;
        setMatrix(newMatrix);
    };

    const loadExample = (exampleNumber: number) => {
        switch (exampleNumber) {
            case 1:
                setN(3);
                setM(3);
                setP(3);
                setTimeout(() => {
                    setMatrix([
                        ['3', '2', '2'],
                        ['2', '2', '2'],
                        ['2', '2', '1']
                    ]);
                }, 100);
                break;
            case 2:
                setN(3);
                setM(4);
                setP(3);
                setTimeout(() => {
                    setMatrix([
                        ['2', '1', '1', '1'],
                        ['1', '1', '1', '1'],
                        ['2', '1', '1', '3']
                    ]);
                }, 100);
                break;
            case 3:
                setN(3);
                setM(4);
                setP(12);
                setTimeout(() => {
                    setMatrix([
                        ['1', '2', '3', '4'],
                        ['8', '7', '6', '5'],
                        ['9', '10', '11', '12']
                    ]);
                }, 100);
                break;
        }
    };

    const generateRandomData = async () => {
        setError('');
        setLoading(true);
        
        try {
            // Use current form values for n, m, p
            const response = await axios.get(`http://localhost:5001/api/generate-random-data?n=${n}&m=${m}&p=${p}`);
            const randomData = response.data;
            
            // Update the form with the generated data
            setN(randomData.n);
            setM(randomData.m);
            setP(randomData.p);
            
            // Convert the matrix to string format for the UI
            setTimeout(() => {
                const stringMatrix = randomData.matrix.map((row: number[]) => 
                    row.map((cell: number) => cell.toString())
                );
                setMatrix(stringMatrix);
            }, 100);
            
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to generate random data');
        } finally {
            setLoading(false);
        }
    };

    const handleHistoryItemClick = async (item: TreasureHuntResult) => {
        try {
            const response = await axios.get(`http://localhost:5001/api/treasure-hunt/${item.id}`);
            const resultWithPath: TreasureHuntResultWithPath = response.data;
            
            // Set the form values to match the selected item
            setN(resultWithPath.n);
            setM(resultWithPath.m);
            setP(resultWithPath.p);
            
            // Convert matrix to string format for display
            setTimeout(() => {
                const stringMatrix = resultWithPath.matrix.map(row => 
                    row.map(cell => cell.toString())
                );
                setMatrix(stringMatrix);
            }, 100);
            
            // Set the path and result
            setCurrentPath(resultWithPath.path);
            setResult(resultWithPath.minFuel);
            setSelectedHistoryItem(resultWithPath);
            setError('');
        } catch (err: any) {
            setError('Failed to load treasure hunt details');
        }
    };

    const getPathStepAtPosition = (row: number, col: number): PathStep | null => {
        const pathToUse = selectedHistoryItem ? selectedHistoryItem.path : currentPath;
        return pathToUse.find(step => step.row === row && step.col === col) || null;
    };

    const getCellStyle = (row: number, col: number) => {
        const step = getPathStepAtPosition(row, col);
        if (!step) return {};
        
        if (step.chestNumber === 0) {
            // Starting position
            return {
                backgroundColor: '#4caf50',
                color: 'white',
                fontWeight: 'bold'
            };
        } else {
            // Path position - use gradient from light blue to dark blue based on step order
            const intensity = Math.min(step.chestNumber / (p || 1), 1);
            const alpha = 0.3 + (intensity * 0.5);
            return {
                backgroundColor: `rgba(33, 150, 243, ${alpha})`,
                color: intensity > 0.5 ? 'white' : 'black',
                fontWeight: 'bold'
            };
        }
    };

    // Function to convert fuel values to mathematical expressions
    const formatFuelAsMath = (fuelUsed: number): string => {
        if (fuelUsed === 0) return '0';
        
        // Check for common perfect squares and their square roots
        const tolerance = 1e-10;
        
        // Check if it's a perfect integer
        if (Math.abs(fuelUsed - Math.round(fuelUsed)) < tolerance) {
            return Math.round(fuelUsed).toString();
        }
        
        // Check for common square roots
        const squaredValue = fuelUsed * fuelUsed;
        const roundedSquared = Math.round(squaredValue);
        
        if (Math.abs(squaredValue - roundedSquared) < tolerance) {
            if (roundedSquared === 2) return '√2';
            if (roundedSquared === 3) return '√3';
            if (roundedSquared === 5) return '√5';
            if (roundedSquared === 6) return '√6';
            if (roundedSquared === 7) return '√7';
            if (roundedSquared === 8) return '2√2';
            if (roundedSquared === 10) return '√10';
            if (roundedSquared === 12) return '2√3';
            if (roundedSquared === 13) return '√13';
            if (roundedSquared === 18) return '3√2';
            if (roundedSquared === 20) return '2√5';
            if (roundedSquared === 50) return '5√2';
            
            // For other perfect squares
            const sqrt = Math.sqrt(roundedSquared);
            if (Math.abs(sqrt - Math.round(sqrt)) < tolerance) {
                return Math.round(sqrt).toString();
            } else {
                return `√${roundedSquared}`;
            }
        }
        
        // If no clean mathematical expression, show as decimal with limited precision
        return fuelUsed.toFixed(3);
    };

    return (
        <Container maxWidth="lg" sx={{py: 4}}>
            <Typography variant="h3" component="h1" gutterBottom align="center" color="primary">
                🏴‍☠️ Treasure Hunt Solver
            </Typography>

            <Box sx={{display: 'flex', gap: 3, flexDirection: {xs: 'column', md: 'row'}}}>
                <Box sx={{flex: 1}}>
                    <StyledPaper>
                        <Typography variant="h5" gutterBottom>
                            Input Parameters
                        </Typography>

                        <Box sx={{display: 'flex', gap: 2, mb: 2}}>
                            <TextField
                                fullWidth
                                label="Rows (n)"
                                type="number"
                                value={n}
                                onChange={(e) => setN(Math.max(1, Math.min(500, Number(e.target.value))))}
                                inputProps={{min: 1, max: 500}}
                            />
                            <TextField
                                fullWidth
                                label="Columns (m)"
                                type="number"
                                value={m}
                                onChange={(e) => setM(Math.max(1, Math.min(500, Number(e.target.value))))}
                                inputProps={{min: 1, max: 500}}
                            />
                            <TextField
                                fullWidth
                                label="Max Chest (p)"
                                type="number"
                                value={p}
                                onChange={(e) => setP(Math.max(1, Number(e.target.value)))}
                                inputProps={{min: 1}}
                            />
                        </Box>

                        <Box sx={{mb: 2}}>
                            <Typography variant="h6" gutterBottom>
                                Load Examples:
                            </Typography>
                            <Box sx={{display: 'flex', gap: 1, flexWrap: 'wrap'}}>
                                <Button variant="outlined" size="small" onClick={() => loadExample(1)}>
                                    Example 1
                                </Button>
                                <Button variant="outlined" size="small" onClick={() => loadExample(2)}>
                                    Example 2
                                </Button>
                                <Button variant="outlined" size="small" onClick={() => loadExample(3)}>
                                    Example 3
                                </Button>
                                <Button 
                                    variant="contained" 
                                    size="small" 
                                    color="secondary"
                                    onClick={generateRandomData}
                                    disabled={loading}
                                    startIcon={<Shuffle />}
                                >
                                    Random Data
                                </Button>
                            </Box>
                        </Box>

                        <Box sx={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2}}>
                            <Typography variant="h6">
                                Matrix Input
                            </Typography>
                            <Button
                                variant="contained"
                                size="large"
                                onClick={handleSolve}
                                disabled={loading}
                                sx={{minWidth: '200px'}}
                            >
                                {loading ? 'Solving...' : 'Solve Treasure Hunt'}
                            </Button>
                        </Box>

                        {/* Fixed height and width wrapper for matrix input */}
                        <Box sx={{ 
                            maxHeight: 500,
                            minHeight: 200,
                            maxWidth: '100%',
                            border: '1px solid rgba(0, 0, 0, 0.12)',
                            borderRadius: 1,
                            overflow: 'auto',
                            display: 'flex',
                            flexDirection: 'column',
                            backgroundColor: '#fff',
                            mb: 2
                        }}>
                            <TableContainer component={Paper} variant="outlined" sx={{ 
                                flex: 1,
                                border: 'none',
                                overflow: 'auto',
                                maxHeight: '100%'
                            }}>
                                <Table size="small">
                                    <TableBody>
                                        {Array.from({length: n}, (_, i) => (
                                            <TableRow key={i}>
                                                {Array.from({length: m}, (_, j) => (
                                                    <TableCell 
                                                        key={j} 
                                                        sx={{p: 0.5, minWidth: '70px'}}
                                                    >
                                                        <TextField
                                                            size="small"
                                                            type="number"
                                                            value={matrix[i]?.[j] || ''}
                                                            onChange={(e) => handleMatrixChange(i, j, e.target.value)}
                                                            inputProps={{min: 1, max: p, style: {textAlign: 'center'}}}
                                                            sx={{
                                                                width: '60px',
                                                                '& .MuiOutlinedInput-root': {
                                                                    backgroundColor: matrix[i]?.[j] && Number(matrix[i][j]) === p ? '#ffebee' : 'transparent',
                                                                    '& fieldset': {
                                                                        borderColor: matrix[i]?.[j] && Number(matrix[i][j]) === p ? '#f44336' : undefined,
                                                                        borderWidth: matrix[i]?.[j] && Number(matrix[i][j]) === p ? '2px' : '1px'
                                                                    },
                                                                    '&:hover fieldset': {
                                                                        borderColor: matrix[i]?.[j] && Number(matrix[i][j]) === p ? '#d32f2f' : undefined
                                                                    },
                                                                    '&.Mui-focused fieldset': {
                                                                        borderColor: matrix[i]?.[j] && Number(matrix[i][j]) === p ? '#d32f2f' : undefined
                                                                    }
                                                                },
                                                                '& .MuiOutlinedInput-input': {
                                                                    color: matrix[i]?.[j] && Number(matrix[i][j]) === p ? '#d32f2f' : 'inherit',
                                                                    fontWeight: matrix[i]?.[j] && Number(matrix[i][j]) === p ? 'bold' : 'normal'
                                                                }
                                                            }}
                                                        />
                                                    </TableCell>
                                                ))}
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        </Box>

                        {error && (
                            <Alert severity="error" sx={{mb: 2}}>
                                {error}
                            </Alert>
                        )}

                        {result !== null && (
                            <Card sx={{mt: 2, bgcolor: 'success.light'}}>
                                <CardContent>
                                    <Typography variant="h6" color="success.dark">
                                        Minimum Fuel Required: {result.toFixed(5)}
                                    </Typography>
                                    {(currentPath.length > 0 || (selectedHistoryItem && selectedHistoryItem.path.length > 0)) && (
                                        <Box sx={{mt: 2}}>
                                            <Typography variant="subtitle2" color="success.dark" gutterBottom>
                                                Path Details:
                                            </Typography>
                                            <Typography variant="body2" color="success.dark">
                                                {selectedHistoryItem ? selectedHistoryItem.path.length : currentPath.length} steps total
                                            </Typography>
                                            <Typography variant="caption" color="success.dark" sx={{display: 'block', mt: 1}}>
                                                💚 Green = Start Position, 🔵 Blue shades = Path steps (darker = later steps)
                                            </Typography>
                                        </Box>
                                    )}
                                </CardContent>
                            </Card>
                        )}
                    </StyledPaper>
                </Box>

                <Box sx={{flex: 1}}>
                    <StyledPaper>
                        <Typography variant="h5" gutterBottom>
                            Previous Solutions
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{mb: 1}}>
                            Click on any row to view the solution path on the matrix above
                            {totalCount > 0 && ` • Total: ${totalCount} solutions`}
                        </Typography>
                        <Divider sx={{mb: 2}}/>

                        {history.length === 0 ? (
                            <Box sx={{ 
                                minHeight: 300,
                                maxHeight: 400, 
                                display: 'flex', 
                                alignItems: 'center', 
                                justifyContent: 'center',
                                border: '1px solid rgba(0, 0, 0, 0.12)',
                                borderRadius: 1,
                                backgroundColor: '#fafafa'
                            }}>
                                <Typography color="text.secondary">
                                    No previous solutions found.
                                </Typography>
                            </Box>
                        ) : (
                            <>
                                {/* Fixed max height wrapper for table */}
                                <Box sx={{ 
                                    maxHeight: 400,
                                    minHeight: 300,
                                    border: '1px solid rgba(0, 0, 0, 0.12)',
                                    borderRadius: 1,
                                    overflow: 'hidden',
                                    display: 'flex',
                                    flexDirection: 'column',
                                    backgroundColor: '#fff'
                                }}>
                                    <TableContainer component={Paper} variant="outlined" sx={{ 
                                        flex: 1,
                                        border: 'none',
                                        overflow: 'auto',
                                        maxHeight: '100%'
                                    }}>
                                        <Table size="small" stickyHeader>
                                            <TableHead>
                                                <TableRow>
                                                    <TableCell sx={{ backgroundColor: '#f5f5f5', fontWeight: 'bold' }}>N×M</TableCell>
                                                    <TableCell sx={{ backgroundColor: '#f5f5f5', fontWeight: 'bold' }}>P</TableCell>
                                                    <TableCell sx={{ backgroundColor: '#f5f5f5', fontWeight: 'bold' }}>Min Fuel</TableCell>
                                                    <TableCell sx={{ backgroundColor: '#f5f5f5', fontWeight: 'bold' }}>Date</TableCell>
                                                </TableRow>
                                            </TableHead>
                                            <TableBody>
                                                {history.map((item) => (
                                                <TableRow 
                                                    key={item.id} 
                                                    onClick={() => handleHistoryItemClick(item)} 
                                                    sx={{
                                                        cursor: 'pointer', 
                                                        '&:hover': {backgroundColor: '#f5f5f5'},
                                                        backgroundColor: selectedHistoryItem?.id === item.id ? '#e3f2fd' : 'inherit'
                                                    }}
                                                >
                                                    <TableCell>{item.n}×{item.m}</TableCell>
                                                    <TableCell>{item.p}</TableCell>
                                                    <TableCell>{item.minFuel.toFixed(5)}</TableCell>
                                                    <TableCell>
                                                        {new Date(item.createdAt).toLocaleDateString()}
                                                    </TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                                </Box>
                                
                                {/* Pagination */}
                                {totalPages > 1 && (
                                    <Box sx={{display: 'flex', justifyContent: 'center', mt: 2}}>
                                        <Pagination
                                            count={totalPages}
                                            page={historyPage}
                                            onChange={(event, value) => setHistoryPage(value)}
                                            color="primary"
                                            size="small"
                                        />
                                    </Box>
                                )}
                            </>
                        )}
                    </StyledPaper>
                </Box>
            </Box>
            
            {/* Solution Path - Full width below entire interface */}
            {(result !== null || selectedHistoryItem) && (
                <StyledPaper>
                    <Typography variant="h6" gutterBottom>
                        Solution Path
                    </Typography>

                    {selectedHistoryItem && (
                        <Button
                            variant="outlined"
                            onClick={() => setSelectedHistoryItem(null)}
                            sx={{mb: 2}}
                            size="small"
                        >
                            View Current Solution
                        </Button>
                    )}

                    <TableContainer component={Paper} variant="outlined">
                        <Table size="small">
                            <TableBody>
                                {Array.from({length: n}, (_, i) => (
                                    <TableRow key={i}>
                                        {Array.from({length: m}, (_, j) => {
                                            const cellStyle = getCellStyle(i, j);
                                            const pathStep = getPathStepAtPosition(i, j);
                                            const cellValue = selectedHistoryItem ? selectedHistoryItem.matrix[i][j] : (matrix[i] && matrix[i][j] ? Number(matrix[i][j]) : 0);
                                            const isTreasureChest = cellValue === p;
                                            
                                            return (
                                                <TableCell key={j} sx={{p: 0.5, width: '120px', minWidth: '120px', ...cellStyle}}>
                                                    <div style={{ 
                                                        textAlign: 'center', 
                                                        fontSize: '14px', 
                                                        fontWeight: 'bold',
                                                        color: isTreasureChest ? '#d32f2f' : (cellStyle.color || 'inherit'),
                                                        backgroundColor: isTreasureChest ? '#ffebee' : (cellStyle.backgroundColor || 'transparent'),
                                                        padding: '2px 4px',
                                                        borderRadius: '4px',
                                                        border: isTreasureChest ? '2px solid #f44336' : 'none'
                                                    }}>
                                                        {cellValue}
                                                    </div>
                                                    {pathStep && (
                                                        <Typography 
                                                            variant="caption" 
                                                            sx={{
                                                                display: 'block',
                                                                fontSize: '10px',
                                                                lineHeight: 1,
                                                                mt: 0.5,
                                                                color: cellStyle.color || 'inherit',
                                                                textAlign: 'center'
                                                            }}
                                                        >
                                                            {pathStep.chestNumber === 0 
                                                                ? 'Start Position'
                                                                : `Step ${pathStep.chestNumber} | Fuel Used: ${formatFuelAsMath(pathStep.fuelUsed)}`
                                                            }
                                                        </Typography>
                                                    )}
                                                </TableCell>
                                            );
                                        })}
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                </StyledPaper>
            )}
        </Container>
    );
};

export default App;