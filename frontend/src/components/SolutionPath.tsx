import React from 'react';
import {
    Box,
    Typography,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableRow
} from '@mui/material';
import { PathStep } from '../types';

interface SolutionPathProps {
    n: number;
    m: number;
    p: number;
    matrix: string[][];
    selectedHistoryMatrix?: number[][];
    currentPath: PathStep[];
    selectedHistoryPath?: PathStep[];
    result: number | null;
    selectedHistoryItem: any;
    formatFuelAsMath: (fuel: number) => string;
}

const SolutionPath: React.FC<SolutionPathProps> = ({
    n,
    m,
    p,
    matrix,
    selectedHistoryMatrix,
    currentPath,
    selectedHistoryPath,
    result,
    selectedHistoryItem,
    formatFuelAsMath
}) => {
    const getPathStepAtPosition = (row: number, col: number): PathStep | null => {
        const pathToUse = selectedHistoryItem ? selectedHistoryPath || [] : currentPath;
        return pathToUse.find(step => step.row === row && step.col === col) || null;
    };

    const getCellStyle = (row: number, col: number) => {
        const step = getPathStepAtPosition(row, col);
        if (!step) return {};
        
        if (step.chestNumber === 0) {
            return {
                backgroundColor: '#4caf50',
                color: 'white',
                fontWeight: 'bold'
            };
        } else {
            const intensity = Math.min(step.chestNumber / (p || 1), 1);
            const alpha = 0.3 + (intensity * 0.5);
            return {
                backgroundColor: `rgba(33, 150, 243, ${alpha})`,
                color: intensity > 0.5 ? 'white' : 'black',
                fontWeight: 'bold'
            };
        }
    };

    if (result === null && !selectedHistoryItem) {
        return null;
    }

    return (
        <Paper sx={{ padding: 3, marginBottom: 2 }}>
            <Typography variant="h6" gutterBottom>
                Solution Path
            </Typography>

            <TableContainer 
                component={Paper} 
                variant="outlined" 
                sx={{ 
                    maxHeight: 500,
                    overflowY: 'auto'
                }}
            >
                <Table size="small">
                    <TableBody>
                        {Array.from({length: n}, (_, i) => (
                            <TableRow key={i}>
                                {Array.from({length: m}, (_, j) => {
                                    const cellStyle = getCellStyle(i, j);
                                    const pathStep = getPathStepAtPosition(i, j);
                                    const cellValue = selectedHistoryItem 
                                        ? selectedHistoryMatrix?.[i]?.[j] || 0
                                        : (matrix[i] && matrix[i][j] ? Number(matrix[i][j]) : 0);
                                    const isTreasureChest = cellValue === p;
                                    
                                    return (
                                        <TableCell 
                                            key={j} 
                                            sx={{
                                                p: 0.5, 
                                                width: '120px', 
                                                minWidth: '120px', 
                                                textAlign: 'center',
                                                position: 'relative',
                                                backgroundColor: isTreasureChest ? '#ffebee' : 'inherit',
                                                border: isTreasureChest ? '2px solid #f44336' : undefined,
                                                ...cellStyle
                                            }}
                                        >
                                            <Box sx={{ 
                                                display: 'flex', 
                                                flexDirection: 'column', 
                                                alignItems: 'center',
                                                justifyContent: 'center',
                                                minHeight: '60px'
                                            }}>
                                                <Typography 
                                                    variant="h6" 
                                                    sx={{
                                                        fontWeight: 'bold',
                                                        color: isTreasureChest ? '#d32f2f' : (cellStyle.color || 'inherit')
                                                    }}
                                                >
                                                    {cellValue}
                                                </Typography>
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
                                                            : `Step ${pathStep.chestNumber} (Cumulative Fuel: ${formatFuelAsMath(pathStep.cumulativeFuel)})`
                                                        }
                                                    </Typography>
                                                )}
                                            </Box>
                                        </TableCell>
                                    );
                                })}
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        </Paper>
    );
};

export default SolutionPath;
