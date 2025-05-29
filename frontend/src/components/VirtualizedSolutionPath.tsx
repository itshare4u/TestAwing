import React, { useMemo, useCallback } from 'react';
import {
    Box,
    Typography,
    Paper
} from '@mui/material';
import { FixedSizeGrid } from 'react-window';
import { PathStep } from '../types';

interface VirtualizedSolutionPathProps {
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

// Constants for virtual grid sizing - smaller than the matrix input
const CELL_WIDTH = 70;
const CELL_HEIGHT = 60;
const MAX_GRID_HEIGHT = 400; // Smaller than full screen
const CELL_PADDING = 2;

const VirtualizedSolutionPath: React.FC<VirtualizedSolutionPathProps> = ({
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
    // Memoize path lookups for performance
    const pathLookup = useMemo(() => {
        const pathToUse = selectedHistoryItem ? selectedHistoryPath || [] : currentPath;
        const lookup = new Map<string, PathStep>();
        pathToUse.forEach(step => {
            lookup.set(`${step.row}-${step.col}`, step);
        });
        return lookup;
    }, [selectedHistoryItem, selectedHistoryPath, currentPath]);

    const getPathStepAtPosition = useCallback((row: number, col: number): PathStep | null => {
        return pathLookup.get(`${row}-${col}`) || null;
    }, [pathLookup]);

    const getCellStyle = useCallback((row: number, col: number) => {
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
    }, [getPathStepAtPosition, p]);

    // Calculate grid dimensions
    const { totalWidth, totalHeight, hasHorizontalScroll, hasVerticalScroll } = useMemo(() => {
        const totalWidth = Math.min(m * CELL_WIDTH, 800); // Max width constraint
        const totalHeight = Math.min(n * CELL_HEIGHT, MAX_GRID_HEIGHT);
        const hasHorizontalScroll = m * CELL_WIDTH > 800;
        const hasVerticalScroll = n * CELL_HEIGHT > MAX_GRID_HEIGHT;
        
        return { totalWidth, totalHeight, hasHorizontalScroll, hasVerticalScroll };
    }, [n, m]);

    // Virtual grid cell renderer
    const Cell = useCallback(({ columnIndex, rowIndex, style }: any) => {
        const cellStyle = getCellStyle(rowIndex, columnIndex);
        const pathStep = getPathStepAtPosition(rowIndex, columnIndex);
        const cellValue = selectedHistoryItem 
            ? selectedHistoryMatrix?.[rowIndex]?.[columnIndex] || 0
            : (matrix[rowIndex] && matrix[rowIndex][columnIndex] ? Number(matrix[rowIndex][columnIndex]) : 0);
        const isTreasureChest = cellValue === p;
        
        return (
            <Box 
                style={{
                    ...style,
                    padding: CELL_PADDING,
                    boxSizing: 'border-box'
                }}
            >
                <Box sx={{
                    width: CELL_WIDTH - (2 * CELL_PADDING),
                    height: CELL_HEIGHT - (2 * CELL_PADDING),
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    backgroundColor: isTreasureChest ? '#ffebee' : 'inherit',
                    border: isTreasureChest ? '2px solid #f44336' : '1px solid rgba(0, 0, 0, 0.12)',
                    borderRadius: '4px',
                    position: 'relative',
                    ...cellStyle
                }}>
                    <Typography 
                        variant="body2" 
                        sx={{
                            fontWeight: 'bold',
                            color: isTreasureChest ? '#d32f2f' : (cellStyle.color || 'inherit'),
                            fontSize: '14px'
                        }}
                    >
                        {cellValue}
                    </Typography>
                    {pathStep && (
                        <Typography 
                            variant="caption" 
                            sx={{
                                display: 'block',
                                fontSize: '9px',
                                lineHeight: 1,
                                mt: 0.5,
                                color: cellStyle.color || 'inherit',
                                textAlign: 'center',
                                maxWidth: '100%',
                                overflow: 'hidden',
                                textOverflow: 'ellipsis'
                            }}
                        >
                            {pathStep.chestNumber === 0 
                                ? 'Start'
                                : `${pathStep.chestNumber}: ${formatFuelAsMath(pathStep.cumulativeFuel || pathStep.fuelUsed)}`
                            }
                        </Typography>
                    )}
                </Box>
            </Box>
        );
    }, [getCellStyle, getPathStepAtPosition, selectedHistoryItem, selectedHistoryMatrix, matrix, p, formatFuelAsMath]);

    // Memoize getItemKey
    const getItemKey = useCallback(({ columnIndex, rowIndex }: { columnIndex: number, rowIndex: number }) => 
        `${rowIndex}-${columnIndex}`, []);

    if (result === null && !selectedHistoryItem) {
        return null;
    }

    return (
        <Paper sx={{ padding: 2, marginTop: 2 }}>
            <Typography variant="h6" gutterBottom>
                Solution Path
            </Typography>

            <Box sx={{
                border: '1px solid rgba(0, 0, 0, 0.12)',
                borderRadius: 1,
                overflow: 'hidden',
                position: 'relative',
                backgroundColor: '#fff'
            }}>
                {(hasHorizontalScroll || hasVerticalScroll) && (
                    <Box sx={{
                        position: 'absolute',
                        top: 8,
                        right: 8,
                        backgroundColor: 'rgba(0, 0, 0, 0.1)',
                        padding: '4px 8px',
                        borderRadius: 1,
                        fontSize: '0.75rem',
                        zIndex: 1
                    }}>
                        {n}×{m} matrix {hasHorizontalScroll ? '(scroll →)' : ''}
                    </Box>
                )}
                
                <FixedSizeGrid
                    columnCount={m}
                    columnWidth={CELL_WIDTH}
                    height={totalHeight}
                    rowCount={n}
                    rowHeight={CELL_HEIGHT}
                    width={totalWidth}
                    itemKey={getItemKey}
                >
                    {Cell}
                </FixedSizeGrid>
            </Box>

            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                Matrix size: {n}×{m} = {n * m} cells • Using virtualized view for optimal performance
            </Typography>
        </Paper>
    );
};

export default React.memo(VirtualizedSolutionPath);
