import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import App from '../../../App';
import axios from 'axios';

// Mock axios
jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

describe('Treasure Hunt Integration Flow', () => {
    beforeEach(() => {
        // Clear all mocks before each test
        jest.clearAllMocks();
        
        // Setup default axios responses
        mockedAxios.get.mockImplementation((url) => {
            if (url.includes('treasure-hunts')) {
                return Promise.resolve({
                    data: {
                        data: [],
                        totalPages: 0,
                        totalCount: 0
                    }
                });
            }
            return Promise.reject(new Error('Not found'));
        });
    });

    it('creates and fills matrix, then solves treasure hunt', async () => {
        // Mock successful solve response
        mockedAxios.post.mockResolvedValueOnce({
            data: {
                minFuel: 4,
                path: [
                    { row: 0, col: 0, chestNumber: 1, fuelUsed: 0 },
                    { row: 1, col: 1, chestNumber: 2, fuelUsed: 2 },
                    { row: 2, col: 2, chestNumber: 3, fuelUsed: 4 }
                ]
            }
        });

        render(<App />);

        // Set matrix dimensions
        const nInput = screen.getByLabelText(/rows/i);
        const mInput = screen.getByLabelText(/columns/i);
        const pInput = screen.getByLabelText(/max chest/i);

        fireEvent.change(nInput, { target: { value: '3' } });
        fireEvent.change(mInput, { target: { value: '3' } });
        fireEvent.change(pInput, { target: { value: '3' } });

        // Create matrix
        const createButton = screen.getByText(/create matrix/i);
        fireEvent.click(createButton);

        // Check if matrix is created
        const inputs = screen.getAllByRole('spinbutton');
        expect(inputs.length).toBe(12); // 3x3 matrix + 3 parameter inputs

        // Fill matrix
        const matrixInputs = inputs.slice(3); // Skip parameter inputs
        matrixInputs.forEach((input, index) => {
            fireEvent.change(input, { target: { value: String((index % 3) + 1) } });
        });

        // Solve puzzle
        const solveButton = screen.getByText(/solve treasure hunt/i);
        fireEvent.click(solveButton);

        // Wait for solve result
        await waitFor(() => {
            expect(screen.getByText(/minimum fuel required: 4/i)).toBeInTheDocument();
        });

        // Verify solution path is displayed
        expect(screen.getByText(/solution path/i)).toBeInTheDocument();
    });

    it('loads example and shows correct matrix values', () => {
        render(<App />);

        // Click example 1 button
        const example1Button = screen.getByText(/example 1/i);
        fireEvent.click(example1Button);

        // Verify matrix dimensions and values
        const inputs = screen.getAllByRole('spinbutton');
        const matrixInputs = inputs.slice(3); // Skip parameter inputs

        const expectedValues = [
            '3', '2', '2',
            '2', '2', '2',
            '2', '2', '1'
        ];

        matrixInputs.forEach((input, index) => {
            expect(input).toHaveValue(Number(expectedValues[index]));
        });
    });

    it('generates random matrix data', async () => {
        // Mock random data response
        mockedAxios.get.mockResolvedValueOnce({
            data: {
                n: 3,
                m: 3,
                p: 3,
                matrix: [
                    [1, 2, 3],
                    [2, 2, 2],
                    [3, 2, 1]
                ]
            }
        });

        render(<App />);

        // Click random button
        const randomButton = screen.getByText(/random/i);
        fireEvent.click(randomButton);

        // Wait for matrix to be populated
        await waitFor(() => {
            const inputs = screen.getAllByRole('spinbutton');
            const matrixInputs = inputs.slice(3); // Skip parameter inputs
            expect(matrixInputs[0]).toHaveValue(1);
            expect(matrixInputs[8]).toHaveValue(1);
        });
    });

    it('shows error message when solving invalid matrix', async () => {
        render(<App />);

        // Try to solve without creating matrix
        const solveButton = screen.getByText(/solve treasure hunt/i);
        fireEvent.click(solveButton);

        // Check error message
        expect(screen.getByText(/please create a matrix first/i)).toBeInTheDocument();
    });

    it('updates history when solution is found', async () => {
        // Mock successful solve response
        mockedAxios.post.mockResolvedValueOnce({
            data: {
                minFuel: 4,
                path: [
                    { row: 0, col: 0, chestNumber: 1, fuelUsed: 0 },
                    { row: 1, col: 1, chestNumber: 2, fuelUsed: 2 },
                    { row: 2, col: 2, chestNumber: 3, fuelUsed: 4 }
                ]
            }
        });

        // Mock updated history response
        mockedAxios.get.mockResolvedValue({
            data: {
                data: [{
                    id: 1,
                    n: 3,
                    m: 3,
                    p: 3,
                    minFuel: 4,
                    createdAt: new Date().toISOString()
                }],
                totalPages: 1,
                totalCount: 1
            }
        });

        render(<App />);

        // Create and fill matrix
        const createButton = screen.getByText(/create matrix/i);
        fireEvent.click(createButton);

        const inputs = screen.getAllByRole('spinbutton');
        const matrixInputs = inputs.slice(3);
        matrixInputs.forEach((input, index) => {
            fireEvent.change(input, { target: { value: String((index % 3) + 1) } });
        });

        // Solve puzzle
        const solveButton = screen.getByText(/solve treasure hunt/i);
        fireEvent.click(solveButton);

        // Wait for history update
        await waitFor(() => {
            expect(screen.getByText(/total: 1 solutions/i)).toBeInTheDocument();
        });
    });
});
