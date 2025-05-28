import React from 'react';
import { Box, TextField, Button, Typography } from '@mui/material';
import { Shuffle, FileUpload, FileDownload } from '@mui/icons-material';

interface ParameterInputProps {
    n: number;
    m: number;
    p: number;
    loading: boolean;
    onNChange: (value: number) => void;
    onMChange: (value: number) => void;
    onPChange: (value: number) => void;
    onCreateMatrix: () => void;
    onGenerateRandom: () => void;
    onLoadExample: (exampleNumber: number) => void;
    onFileUpload: (event: React.ChangeEvent<HTMLInputElement>) => void;
    onExportMatrix: () => void;
    onSolve: () => void;
}

const ParameterInput: React.FC<ParameterInputProps> = ({
    n,
    m,
    p,
    loading,
    onNChange,
    onMChange,
    onPChange,
    onCreateMatrix,
    onGenerateRandom,
    onLoadExample,
    onFileUpload,
    onExportMatrix,
    onSolve
}) => {
    return (
        <>
            <Typography variant="h5" gutterBottom>
                Input Parameters
            </Typography>

            <Box sx={{display: 'flex', gap: 2, mb: 2}}>
                <TextField
                    fullWidth
                    label="Rows (n)"
                    type="number"
                    value={n}
                    onChange={(e) => onNChange(Math.max(1, Math.min(500, Number(e.target.value))))}
                    inputProps={{min: 1, max: 500}}
                />
                <TextField
                    fullWidth
                    label="Columns (m)"
                    type="number"
                    value={m}
                    onChange={(e) => onMChange(Math.max(1, Math.min(500, Number(e.target.value))))}
                    inputProps={{min: 1, max: 500}}
                />
                <TextField
                    fullWidth
                    label="Max Chest (p)"
                    type="number"
                    value={p}
                    onChange={(e) => onPChange(Math.max(1, Number(e.target.value)))}
                    inputProps={{min: 1}}
                />
            </Box>

            <Box sx={{display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap'}}>
                <Button
                    variant="contained"
                    color="primary"
                    onClick={onCreateMatrix}
                    sx={{minWidth: '120px'}}
                >
                    Create Matrix
                </Button>
                <Button
                    variant="outlined"
                    startIcon={<Shuffle />}
                    onClick={onGenerateRandom}
                    disabled={loading}
                >
                    Random
                </Button>
                <Button
                    variant="outlined"
                    onClick={() => onLoadExample(1)}
                    size="small"
                >
                    Example 1
                </Button>
                <Button
                    variant="outlined"
                    onClick={() => onLoadExample(2)}
                    size="small"
                >
                    Example 2
                </Button>
                <Button
                    variant="outlined"
                    onClick={() => onLoadExample(3)}
                    size="small"
                >
                    Example 3
                </Button>
            </Box>

            <Box sx={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2}}>
                <Typography variant="h6">
                    Matrix Input
                </Typography>
                <Box sx={{ display: 'flex', gap: 2 }}>
                    <Button
                        variant="outlined"
                        size="small"
                        startIcon={<FileUpload />}
                        component="label"
                    >
                        Upload Matrix
                        <input
                            hidden
                            type="file"
                            accept=".txt"
                            onChange={onFileUpload}
                        />
                    </Button>
                    <Button
                        variant="outlined"
                        size="small"
                        startIcon={<FileDownload />}
                        onClick={onExportMatrix}
                    >
                        Export Matrix
                    </Button>
                    <Button
                        variant="contained"
                        size="large"
                        onClick={onSolve}
                        disabled={loading}
                        sx={{minWidth: '200px'}}
                    >
                        {loading ? 'Solving...' : 'Solve Treasure Hunt'}
                    </Button>
                </Box>
            </Box>

            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                <Typography variant="body2" color="text.secondary">
                    Matrix size: {n}×{m} = {n * m} cells
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    Using virtualized view for optimal performance
                </Typography>
            </Box>
        </>
    );
};

export default ParameterInput;
