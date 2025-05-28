import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import MatrixInput from '../MatrixInput';

describe('MatrixInput', () => {
    const mockOnMatrixChange = jest.fn();
    const defaultProps = {
        n: 3,
        m: 3,
        p: 3,
        matrix: [
            ['1', '2', '3'],
            ['2', '2', '2'],
            ['3', '2', '1']
        ],
        onMatrixChange: mockOnMatrixChange
    };

    beforeEach(() => {
        mockOnMatrixChange.mockClear();
    });

    it('renders matrix with correct number of cells', () => {
        render(<MatrixInput {...defaultProps} />);
        const inputs = screen.getAllByRole('spinbutton');
        expect(inputs).toHaveLength(9); // 3x3 matrix
    });

    it('displays correct values in cells', () => {
        render(<MatrixInput {...defaultProps} />);
        const inputs = screen.getAllByRole('spinbutton');
        expect(inputs[0]).toHaveValue(1);
        expect(inputs[4]).toHaveValue(2); // Center cell
        expect(inputs[8]).toHaveValue(1); // Bottom right cell
    });

    it('highlights cells with value equal to p', () => {
        render(<MatrixInput {...defaultProps} />);
        const inputs = screen.getAllByRole('spinbutton');
        const cell = inputs[0].closest('.MuiOutlinedInput-root');
        expect(cell).toHaveStyle({ backgroundColor: 'transparent' });
    });

    it('calls onMatrixChange when cell value changes', () => {
        render(<MatrixInput {...defaultProps} />);
        const inputs = screen.getAllByRole('spinbutton');
        fireEvent.change(inputs[0], { target: { value: '2' } });
        expect(mockOnMatrixChange).toHaveBeenCalledWith(0, 0, '2');
    });

    it('does not render when matrix is empty', () => {
        render(<MatrixInput {...defaultProps} matrix={[]} />);
        const inputs = screen.queryAllByRole('spinbutton');
        expect(inputs).toHaveLength(0);
    });

    it('constrains input values between 1 and p', () => {
        render(<MatrixInput {...defaultProps} />);
        const inputs = screen.getAllByRole('spinbutton');
        expect(inputs[0]).toHaveAttribute('min', '1');
        expect(inputs[0]).toHaveAttribute('max', '3');
    });
});
