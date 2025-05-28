import React from 'react';
import { Alert, Card, CardContent, Typography } from '@mui/material';

interface ResultDisplayProps {
    result: number | null;
    error: string;
}

const ResultDisplay: React.FC<ResultDisplayProps> = ({ result, error }) => {
    return (
        <>
            {error && (
                <Alert severity="error" sx={{mb: 2}}>
                    {error}
                </Alert>
            )}

            {result !== null && (
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
