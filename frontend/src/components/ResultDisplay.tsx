import React from 'react';
import { Alert, Box, Card, CardContent, Typography, CircularProgress, LinearProgress } from '@mui/material';
import { SolveStatus } from '../types';

interface ResultDisplayProps {
    result: number | null;
    error: string;
    loading: boolean;
    solveStatus?: SolveStatus;
    currentSolveId?: number | null;
}

const ResultDisplay: React.FC<ResultDisplayProps> = ({ 
    result, 
    error, 
    loading, 
    solveStatus = SolveStatus.Pending,
    currentSolveId
}) => {
    const getStatusMessage = () => {
        switch (solveStatus) {
            case SolveStatus.Pending:
                return 'Starting solve operation...';
            case SolveStatus.InProgress:
                return 'Solving treasure hunt using parallel algorithms...';
            case SolveStatus.Completed:
                return 'Solve operation completed successfully!';
            case SolveStatus.Cancelled:
                return 'Solve operation was cancelled';
            case SolveStatus.Failed:
                return 'Solve operation failed';
            default:
                return 'Solving treasure hunt...';
        }
    };

    const getStatusColor = () => {
        switch (solveStatus) {
            case SolveStatus.Completed:
                return 'success.light';
            case SolveStatus.Cancelled:
                return 'warning.light';
            case SolveStatus.Failed:
                return 'error.light';
            default:
                return 'primary.light';
        }
    };

    return (
        <>
            {error && (
                <Alert severity="error" sx={{mb: 2}}>
                    {error}
                </Alert>
            )}

            {loading && (
                <Card sx={{mt: 2, bgcolor: getStatusColor()}}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                            {(solveStatus === SolveStatus.Pending || solveStatus === SolveStatus.InProgress) && (
                                <CircularProgress size={24} />
                            )}
                            <Typography variant="h6" color="primary.contrastText">
                                {getStatusMessage()}
                            </Typography>
                        </Box>
                        {currentSolveId && (
                            <Typography variant="body2" color="primary.contrastText" sx={{mt: 1, pl: 4}}>
                                Solve ID: {currentSolveId}
                            </Typography>
                        )}
                        <LinearProgress sx={{ mt: 2 }} />
                    </CardContent>
                </Card>
            )}

            {!loading && result !== null && (
                <Card sx={{mt: 2, bgcolor: 'success.light'}}>
                    <CardContent>
                        <Typography variant="h6" color="success.contrastText">
                            🎯 Minimum Fuel Required: {result}
                        </Typography>
                        <Typography variant="body2" color="success.contrastText" sx={{mt: 1}}>
                            The treasure hunt has been solved! Check the solution path below.
                        </Typography>
                    </CardContent>
                </Card>
            )}
        </>
    );
};

export default ResultDisplay;
