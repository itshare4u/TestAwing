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
    Pagination,
    Chip,
    IconButton,
    Tooltip
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import { TreasureHuntResult, SolveStatus } from '../types';

interface HistoryProps {
    history: TreasureHuntResult[];
    totalCount: number;
    totalPages: number;
    historyPage: number;
    onHistoryPageChange: (page: number) => void;
    onHistoryItemClick: (item: TreasureHuntResult) => void;
    onRefresh?: () => void;
}

// Helper function to get status display information
const getStatusInfo = (status: SolveStatus) => {
    switch (status) {
        case SolveStatus.Pending:
            return { label: 'Pending', color: 'warning' as const };
        case SolveStatus.InProgress:
            return { label: 'In Progress', color: 'info' as const };
        case SolveStatus.Completed:
            return { label: 'Completed', color: 'success' as const };
        case SolveStatus.Cancelled:
            return { label: 'Cancelled', color: 'default' as const };
        case SolveStatus.Failed:
            return { label: 'Failed', color: 'error' as const };
        default:
            return { label: 'Unknown', color: 'default' as const };
    }
};

const History: React.FC<HistoryProps> = ({
    history,
    totalCount,
    totalPages,
    historyPage,
    onHistoryPageChange,
    onHistoryItemClick,
    onRefresh
}) => {
    return (
        <>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                <Typography variant="h5" gutterBottom sx={{ mb: 0 }}>
                    Previous Solutions
                </Typography>
                {onRefresh && (
                    <Tooltip title="Refresh history">
                        <IconButton onClick={onRefresh} size="small">
                            <RefreshIcon />
                        </IconButton>
                    </Tooltip>
                )}
            </Box>
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
                                        <TableCell>Status</TableCell>
                                        <TableCell>Min Fuel</TableCell>
                                        <TableCell>Created At</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {history.map((item) => {
                                        const statusInfo = getStatusInfo(item.status);
                                        return (
                                            <TableRow 
                                                key={item.id} 
                                                hover 
                                                onClick={() => onHistoryItemClick(item)}
                                                sx={{cursor: 'pointer'}}
                                            >
                                                <TableCell>{item.n}×{item.m}</TableCell>
                                                <TableCell>{item.p}</TableCell>
                                                <TableCell>
                                                    <Chip 
                                                        label={statusInfo.label} 
                                                        color={statusInfo.color}
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                <TableCell sx={{fontWeight: 'bold', color: 'primary.main'}}>
                                                    {item.status === SolveStatus.Completed 
                                                        ? (Number.isInteger(item.minFuel) ? item.minFuel : item.minFuel.toFixed(5))
                                                        : (item.status === SolveStatus.Failed ? (item.errorMessage || 'Failed') : '—')
                                                    }
                                                </TableCell>
                                                <TableCell>
                                                    {new Date(item.createdAt).toLocaleString()}
                                                </TableCell>
                                            </TableRow>
                                        );
                                    })}
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
