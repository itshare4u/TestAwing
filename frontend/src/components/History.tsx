import React from 'react';
import {
    Box,
    Typography,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Divider,
    Pagination
} from '@mui/material';
import { TreasureHuntResult } from '../types';

interface HistoryProps {
    history: TreasureHuntResult[];
    totalCount: number;
    totalPages: number;
    historyPage: number;
    onHistoryPageChange: (page: number) => void;
    onHistoryItemClick: (item: TreasureHuntResult) => void;
}

const History: React.FC<HistoryProps> = ({
    history,
    totalCount,
    totalPages,
    historyPage,
    onHistoryPageChange,
    onHistoryItemClick
}) => {
    return (
        <>
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
                                        <TableCell>Matrix Size</TableCell>
                                        <TableCell>Max Chest (p)</TableCell>
                                        <TableCell>Min Fuel</TableCell>
                                        <TableCell>Created At</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {history.map((item) => (
                                        <TableRow 
                                            key={item.id} 
                                            hover 
                                            onClick={() => onHistoryItemClick(item)}
                                            sx={{cursor: 'pointer'}}
                                        >
                                            <TableCell>{item.n}×{item.m}</TableCell>
                                            <TableCell>{item.p}</TableCell>
                                            <TableCell sx={{fontWeight: 'bold', color: 'primary.main'}}>
                                                {item.minFuel}
                                            </TableCell>
                                            <TableCell>
                                                {new Date(item.createdAt).toLocaleString()}
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </Box>
                    
                    {totalPages > 1 && (
                        <Box sx={{display: 'flex', justifyContent: 'center', mt: 2}}>
                            <Pagination
                                count={totalPages}
                                page={historyPage}
                                onChange={(event, value) => onHistoryPageChange(value)}
                                color="primary"
                                size="small"
                            />
                        </Box>
                    )}
                </>
            )}
        </>
    );
};

export default History;
