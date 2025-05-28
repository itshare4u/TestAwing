import React from 'react';
import { render, screen } from '@testing-library/react';
import SolutionPath from '../SolutionPath';

describe('SolutionPath', () => {
    const defaultProps = {
        n: 3,
        m: 3,
        p: 3,
        matrix: [['1', '2', '3'], ['2', '2', '2'], ['3', '2', '1']],
        currentPath: [
            { row: 0, col: 0, chestNumber: 1, fuelUsed: 0 },
            { row: 1, col: 1, chestNumber: 2, fuelUsed: 1 },
            { row: 2, col: 2, chestNumber: 3, fuelUsed: 2 }
        ],
        selectedHistoryMatrix: undefined,
        selectedHistoryPath: undefined,
        result: 2,
        selectedHistoryItem: null,
        formatFuelAsMath: (fuel: number) => `√${fuel}`
    };

    it('renders the matrix with path visualization', () => {
        render(<SolutionPath {...defaultProps} />);
        
        // Check if all matrix cells are rendered
        defaultProps.matrix.flat().forEach(value => {
            expect(screen.getByText(value)).toBeInTheDocument();
        });

        // Check if path steps are shown
        expect(screen.getByText('Start')).toBeInTheDocument();
        expect(screen.getByText('√1')).toBeInTheDocument();
        expect(screen.getByText('√2')).toBeInTheDocument();
    });

    it('shows total fuel cost', () => {
        render(<SolutionPath {...defaultProps} />);
        expect(screen.getByText(/total fuel cost/i)).toBeInTheDocument();
        expect(screen.getByText('√2')).toBeInTheDocument();
    });

    it('handles empty path', () => {
        render(<SolutionPath {...defaultProps} currentPath={[]} />);
        expect(screen.queryByText('Start')).not.toBeInTheDocument();
    });

    it('uses history matrix and path when selected', () => {
        const historyMatrix = [['3', '2', '1'], ['2', '2', '2'], ['1', '2', '3']];
        const historyPath = [
            { row: 0, col: 0, chestNumber: 3, fuelUsed: 0 },
            { row: 1, col: 1, chestNumber: 2, fuelUsed: 1 }
        ];

        render(<SolutionPath 
            {...defaultProps}
            selectedHistoryMatrix={historyMatrix}
            selectedHistoryPath={historyPath}
            selectedHistoryItem={{}} 
        />);

        // Check if history matrix is rendered
        historyMatrix.flat().forEach(value => {
            expect(screen.getByText(value)).toBeInTheDocument();
        });
    });

    it('correctly formats fuel values', () => {
        render(<SolutionPath {...defaultProps} />);
        defaultProps.currentPath.forEach(step => {
            if (step.fuelUsed > 0) {
                expect(screen.getByText(`√${step.fuelUsed}`)).toBeInTheDocument();
            }
        });
    });

    it('highlights the treasure chest value', () => {
        render(<SolutionPath {...defaultProps} />);
        const treasureCell = screen.getByText('3');
        const cellContainer = treasureCell.closest('td');
        expect(cellContainer).toHaveStyle({
            backgroundColor: expect.stringContaining('success')
        });
    });
});
