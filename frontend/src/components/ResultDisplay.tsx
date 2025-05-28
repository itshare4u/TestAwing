import React from 'react';
import { Alert, Box, Card, CardContent, Typography, CircularProgress } from '@mui/material';

interface ResultDisplayProps {
    result: number | null;
    error: string;
    loading: boolean;
}

const ResultDisplay: React.FC<ResultDisplayProps> = ({ result, error, loading }) => {
    return (
        <>
            {error && (
                <Alert severity="error" sx={{mb: 2}}>
                    {error}
                </Alert>
            )}

            {loading && (
                <Card sx={{mt: 2, bgcolor: 'primary.light'}}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                            <CircularProgress size={24} />
                            <Typography variant="h6" color="primary.contrastText">
                                Solving treasure hunt...
                            </Typography>
                        </Box>
                        <Typography variant="body2" color="primary.contrastText" sx={{mt: 1, pl: 4}}>
                            Finding the optimal path with minimum fuel...
                        </Typography>
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
