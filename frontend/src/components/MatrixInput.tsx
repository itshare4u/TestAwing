import React, { useEffect, useState, useMemo, useCallback } from 'react';
import { Box, TextField } from '@mui/material';
import { FixedSizeGrid } from 'react-window';

interface MatrixInputProps {
    n: number;
    m: number;
    p: number;
    matrix: string[][];
    onMatrixChange: (row: number, col: number, value: string) => void;
}

// Constants for matrix sizing
const CELL_WIDTH = 70;
const CELL_HEIGHT = 60;
const MAX_GRID_HEIGHT = 600;
const CELL_PADDING = 4;

const MatrixInput: React.FC<MatrixInputProps> = ({
    n,
    m,
    p,
    matrix,
    onMatrixChange
}) => {
    const [parentWidth, setParentWidth] = useState(0);
    const containerRef = React.useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!containerRef.current?.parentElement) return;
        
        const resizeObserver = new ResizeObserver(entries => {
            for (let entry of entries) {
                setParentWidth(entry.contentRect.width);
            }
        });
        
        resizeObserver.observe(containerRef.current.parentElement);
        return () => resizeObserver.disconnect();
    }, []);

    // Memoize dimensions calculations
    const { rows, cols, totalWidth, totalHeight, hasHorizontalScroll, hasVerticalScroll } = useMemo(() => {
        const rows = matrix.length;
        const cols = rows > 0 ? matrix[0].length : 0;
        const totalWidth = Math.min(cols * CELL_WIDTH, parentWidth || 0);
        const totalHeight = Math.min(rows * CELL_HEIGHT, MAX_GRID_HEIGHT);
        const hasHorizontalScroll = cols * CELL_WIDTH > (parentWidth || 0);
        const hasVerticalScroll = rows * CELL_HEIGHT > MAX_GRID_HEIGHT;
        
        return { rows, cols, totalWidth, totalHeight, hasHorizontalScroll, hasVerticalScroll };
    }, [matrix, parentWidth]);

    // Memoize Cell component
    const Cell = useCallback(({ columnIndex, rowIndex, style }: any) => (
        <Box style={{ ...style, padding: CELL_PADDING }}>
            <TextField
                fullWidth
                size="small"
                variant="outlined"
                value={matrix[rowIndex][columnIndex] || ''}
                onChange={(e) => onMatrixChange(rowIndex, columnIndex, e.target.value)}
                inputProps={{
                    style: { textAlign: 'center', padding: '8px' }
                }}
                sx={{
                    '& .MuiOutlinedInput-root': {
                        height: CELL_HEIGHT - (2 * CELL_PADDING),
                        width: CELL_WIDTH - (2 * CELL_PADDING)
                    }
                }}
            />
        </Box>
    ), [matrix, onMatrixChange]);

    // Memoize getItemKey
    const getItemKey = useCallback(({ columnIndex, rowIndex }: { columnIndex: number, rowIndex: number }) => 
        `${rowIndex}-${columnIndex}`, []);

    if (rows === 0 || cols === 0) {
        return null;
    }

    return (
        <Box ref={containerRef} sx={{ 
            maxHeight: MAX_GRID_HEIGHT,
            minHeight: Math.min(200, rows * CELL_HEIGHT),
            maxWidth: parentWidth,
            width: '100%',
            border: '1px solid rgba(0, 0, 0, 0.12)',
            borderRadius: 1,
            overflow: 'hidden',
            display: 'flex',
            flexDirection: 'column',
            backgroundColor: '#fff',
            mb: 2,
            position: 'relative'
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
                    {rows}×{cols} matrix {hasHorizontalScroll ? '(scroll →)' : ''}
                </Box>
            )}
            
            <FixedSizeGrid
                columnCount={cols}
                columnWidth={CELL_WIDTH}
                height={totalHeight}
                rowCount={rows}
                rowHeight={CELL_HEIGHT}
                width={totalWidth}
                itemKey={getItemKey}
            >
                {Cell}
            </FixedSizeGrid>
        </Box>
    );
};

export default React.memo(MatrixInput);
