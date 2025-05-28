import React from 'react';
import { Box, TextField } from '@mui/material';
import { FixedSizeGrid } from 'react-window';

interface MatrixInputProps {
    n: number;
    m: number;
    p: number;
    matrix: string[][];
    onMatrixChange: (row: number, col: number, value: string) => void;
}

const MatrixInput: React.FC<MatrixInputProps> = ({
    n,
    m,
    p,
    matrix,
    onMatrixChange
}) => {
    // Use matrix dimensions instead of n,m props
    const rows = matrix.length;
    const cols = rows > 0 ? matrix[0].length : 0;

    // Define cell renderer for virtualized matrix
    const Cell = ({ columnIndex, rowIndex, style }: { 
        columnIndex: number; 
        rowIndex: number; 
        style: React.CSSProperties; 
    }) => {
        // Only show cells that exist in the matrix
        if (rowIndex >= rows || columnIndex >= cols) {
            return null;
        }

        const isHighlighted = matrix[rowIndex]?.[columnIndex] && Number(matrix[rowIndex][columnIndex]) === p;
        
        return (
            <div style={{
                ...style,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                padding: 4
            }}>
                <TextField
                    size="small"
                    type="number"
                    value={matrix[rowIndex][columnIndex] || ''}
                    onChange={(e) => onMatrixChange(rowIndex, columnIndex, e.target.value)}
                    inputProps={{min: 1, max: p, style: {textAlign: 'center'}}}
                    sx={{
                        width: '60px',
                        '& .MuiOutlinedInput-root': {
                            backgroundColor: isHighlighted ? '#ffebee' : 'transparent',
                            '& fieldset': {
                                borderColor: isHighlighted ? '#f44336' : undefined,
                                borderWidth: isHighlighted ? '2px' : '1px'
                            },
                            '&:hover fieldset': {
                                borderColor: isHighlighted ? '#d32f2f' : undefined
                            },
                            '&.Mui-focused fieldset': {
                                borderColor: isHighlighted ? '#d32f2f' : undefined
                            }
                        },
                        '& .MuiOutlinedInput-input': {
                            color: isHighlighted ? '#d32f2f' : 'inherit',
                            fontWeight: isHighlighted ? 'bold' : 'normal'
                        }
                    }}
                />
            </div>
        );
    };

    // Don't render if matrix is empty
    if (rows === 0 || cols === 0) {
        return null;
    }

    return (
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
            <Box sx={{ height: 500, width: '100%' }}>
                <FixedSizeGrid
                    columnCount={cols}
                    columnWidth={70}
                    height={500}
                    rowCount={rows}
                    rowHeight={60}
                    width={Math.min(cols * 70, window.innerWidth - 100)}
                >
                    {Cell}
                </FixedSizeGrid>
            </Box>
        </Box>
    );
};

export default MatrixInput;
