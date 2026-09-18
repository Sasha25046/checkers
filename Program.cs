using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CheckersLab
{

    public enum Piece
    {
        Empty = 0,
        White = 1,
        WhiteKing = 2,
        Black = -1,
        BlackKing = -2
    }

    public enum MoveOrder
    {
        Natural,
        SortedCaptures,
        Worst,
        Random
    }

    public class Move
    {
        public int FromRow { get; }
        public int FromCol { get; }

        public int ToRow { get; }
        public int ToCol { get; }

        public int CaptureRow { get; }
        public int CaptureCol { get; }

        public Piece CapturedPiece { get; }

        public bool BecameKing { get; }

        public bool IsCapture => CaptureRow >= 0 && CaptureCol >= 0;

        public Move(
            int fromRow,
            int fromCol,
            int toRow,
            int toCol,
            int captureRow = -1,
            int captureCol = -1,
            Piece capturedPiece = Piece.Empty,
            bool becameKing = false)
        {
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
            CaptureRow = captureRow;
            CaptureCol = captureCol;
            CapturedPiece = capturedPiece;
            BecameKing = becameKing;
        }

        public override string ToString()
        {
            string separator = IsCapture ? " x " : " -> ";

            return $"[{FromRow},{FromCol}]{separator}[{ToRow},{ToCol}]";
        }
    }

   
    public class Board
    {
        public const int Size = 6;

        public Piece[,] Cells { get; }

        public bool WhiteTurn { get; set; }

        public Board()
        {
            Cells = new Piece[Size, Size];
            WhiteTurn = true;
        }

        public Board Clone()
        {
            Board copy = new Board();

            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    copy.Cells[r, c] = Cells[r, c];
                }
            }

            copy.WhiteTurn = WhiteTurn;

            return copy;
        }

        
        public static Board InitDefault()
        {
            Board board = new Board();

            // Чорні
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    if ((r + c) % 2 == 1)
                    {
                        board.Cells[r, c] = Piece.Black;
                    }
                }
            }

            // Білі
            for (int r = 4; r < 6; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    if ((r + c) % 2 == 1)
                    {
                        board.Cells[r, c] = Piece.White;
                    }
                }
            }

            board.WhiteTurn = true;

            return board;
        }

       
        public static Board InitTacticalPosition()
        {
            Board board = new Board();

            board.Cells[4, 1] = Piece.White;
            board.Cells[4, 3] = Piece.White;
            board.Cells[5, 4] = Piece.White;

            board.Cells[3, 2] = Piece.Black;
            board.Cells[3, 4] = Piece.Black;
            board.Cells[2, 1] = Piece.Black;
            board.Cells[2, 5] = Piece.Black;

            board.WhiteTurn = true;

            return board;
        }

    
        private static bool Inside(int row, int col)
        {
            return row >= 0 &&
                   row < Size &&
                   col >= 0 &&
                   col < Size;
        }

        
        private static bool IsWhite(Piece piece)
        {
            return piece == Piece.White ||
                   piece == Piece.WhiteKing;
        }

        private static bool IsBlack(Piece piece)
        {
            return piece == Piece.Black ||
                   piece == Piece.BlackKing;
        }

        private static bool IsKing(Piece piece)
        {
            return piece == Piece.WhiteKing ||
                   piece == Piece.BlackKing;
        }

        private static bool IsOwnPiece(Piece piece, bool white)
        {
            return white ? IsWhite(piece) : IsBlack(piece);
        }

        private static bool IsOpponent(Piece piece, bool white)
        {
            return white ? IsBlack(piece) : IsWhite(piece);
        }

        private static readonly int[,] Directions =
        {
            { -1, -1 },
            { -1,  1 },
            {  1, -1 },
            {  1,  1 }
        };


        public List<Move> GetLegalMoves()
        {
            bool white = WhiteTurn;

            List<Move> captures = new List<Move>();
            List<Move> quietMoves = new List<Move>();

            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    Piece piece = Cells[r, c];

                    if (!IsOwnPiece(piece, white))
                    {
                        continue;
                    }

                    GenerateCaptures(
                        r,
                        c,
                        piece,
                        white,
                        captures);

                    GenerateQuietMoves(
                        r,
                        c,
                        piece,
                        white,
                        quietMoves);
                }
            }

            if (captures.Count > 0)
            {
                return captures;
            }

            return quietMoves;
        }

       
        private void GenerateCaptures(
            int row,
            int col,
            Piece piece,
            bool white,
            List<Move> moves)
        {
            for (int i = 0; i < Directions.GetLength(0); i++)
            {
                int dr = Directions[i, 0];
                int dc = Directions[i, 1];

                int middleRow = row + dr;
                int middleCol = col + dc;

                int targetRow = row + 2 * dr;
                int targetCol = col + 2 * dc;

                if (!Inside(middleRow, middleCol) ||
                    !Inside(targetRow, targetCol))
                {
                    continue;
                }

                Piece middle = Cells[middleRow, middleCol];
                Piece target = Cells[targetRow, targetCol];

                if (IsOpponent(middle, white) &&
                    target == Piece.Empty)
                {
                    bool becomesKing =
                        (!IsKing(piece) && white && targetRow == 0) ||
                        (!IsKing(piece) && !white && targetRow == Size - 1);

                    moves.Add(
                        new Move(
                            row,
                            col,
                            targetRow,
                            targetCol,
                            middleRow,
                            middleCol,
                            middle,
                            becomesKing));
                }
            }
        }

        
        private void GenerateQuietMoves(
            int row,
            int col,
            Piece piece,
            bool white,
            List<Move> moves)
        {
            for (int i = 0; i < Directions.GetLength(0); i++)
            {
                int dr = Directions[i, 0];
                int dc = Directions[i, 1];

                if (!IsKing(piece))
                {
                    if (white && dr != -1)
                    {
                        continue;
                    }

                    if (!white && dr != 1)
                    {
                        continue;
                    }
                }

                int targetRow = row + dr;
                int targetCol = col + dc;

                if (!Inside(targetRow, targetCol))
                {
                    continue;
                }

                if (Cells[targetRow, targetCol] != Piece.Empty)
                {
                    continue;
                }

                bool becomesKing =
                    (!IsKing(piece) && white && targetRow == 0) ||
                    (!IsKing(piece) && !white && targetRow == Size - 1);

                moves.Add(
                    new Move(
                        row,
                        col,
                        targetRow,
                        targetCol,
                        -1,
                        -1,
                        Piece.Empty,
                        becomesKing));
            }
        }

       
        public void ApplyMove(Move move)
        {
            Piece piece = Cells[move.FromRow, move.FromCol];

            Cells[move.FromRow, move.FromCol] = Piece.Empty;

            if (move.IsCapture)
            {
                Cells[move.CaptureRow, move.CaptureCol] =
                    Piece.Empty;
            }

            if (move.BecameKing)
            {
                if (piece == Piece.White)
                {
                    piece = Piece.WhiteKing;
                }
                else if (piece == Piece.Black)
                {
                    piece = Piece.BlackKing;
                }
            }

            Cells[move.ToRow, move.ToCol] = piece;

            WhiteTurn = !WhiteTurn;
        }

        public void UndoMove(Move move)
        {
            WhiteTurn = !WhiteTurn;

            Piece piece = Cells[move.ToRow, move.ToCol];

            if (move.BecameKing)
            {
                if (piece == Piece.WhiteKing)
                {
                    piece = Piece.White;
                }
                else if (piece == Piece.BlackKing)
                {
                    piece = Piece.Black;
                }
            }

            Cells[move.ToRow, move.ToCol] = Piece.Empty;

            Cells[move.FromRow, move.FromCol] = piece;

            if (move.IsCapture)
            {
                Cells[move.CaptureRow, move.CaptureCol] =
                    move.CapturedPiece;
            }
        }

       
        public void Print()
        {
            Console.WriteLine();

            Console.WriteLine("    0 1 2 3 4 5");
            Console.WriteLine("  +-------------+");

            for (int r = 0; r < Size; r++)
            {
                Console.Write($"{r} | ");

                for (int c = 0; c < Size; c++)
                {
                    char symbol = Cells[r, c] switch
                    {
                        Piece.White => 'w',
                        Piece.WhiteKing => 'W',
                        Piece.Black => 'b',
                        Piece.BlackKing => 'B',
                        _ => '.'
                    };

                    Console.Write(symbol + " ");
                }

                Console.WriteLine("|");
            }

            Console.WriteLine("  +-------------+");
            Console.WriteLine(
                $"Хід: {(WhiteTurn ? "WHITE" : "BLACK")}");
        }
    }

   
    public class SearchResult
    {
        public int Value { get; set; }

        public Move? BestMove { get; set; }

        public long Nodes { get; set; }

        public long Cutoffs { get; set; }
    }

    
    public static class SearchAlgorithms
    {
        public const int Infinity = 1_000_000;

        // Значення матеріалу
        private const int ManValue = 100;
        private const int KingValue = 350;

        private const int AdvancementBonus = 10;
        private const int CenterBonus = 15;

        public static long PureMinimaxNodes { get; private set; }

        public static long AlphaBetaNodes { get; private set; }

        public static long Cutoffs { get; private set; }

    
        public static int UtilityFunction(Board board)
        {
            int whitePieces = 0;
            int blackPieces = 0;

            int whiteKings = 0;
            int blackKings = 0;

            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    Piece piece = board.Cells[r, c];

                    switch (piece)
                    {
                        case Piece.White:
                            whitePieces++;
                            break;

                        case Piece.WhiteKing:
                            whitePieces++;
                            whiteKings++;
                            break;

                        case Piece.Black:
                            blackPieces++;
                            break;

                        case Piece.BlackKing:
                            blackPieces++;
                            blackKings++;
                            break;
                    }
                }
            }

            if (whitePieces == 0)
            {
                return -10_000;
            }

            if (blackPieces == 0)
            {
                return 10_000;
            }

            List<Move> moves = board.GetLegalMoves();

            if (moves.Count == 0)
            {
                return board.WhiteTurn
                    ? -10_000
                    : 10_000;
            }

            int value =
                (whitePieces - blackPieces) * ManValue +
                (whiteKings - blackKings) *
                (KingValue - ManValue);

            return value;
        }

       
        public static int HeuristicEvaluation(Board board)
        {
            int score = 0;

            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    Piece piece = board.Cells[r, c];

                    if (piece == Piece.Empty)
                    {
                        continue;
                    }

                    int sign =
                        piece == Piece.White ||
                        piece == Piece.WhiteKing
                            ? 1
                            : -1;

                    int pieceValue =
                        Math.Abs((int)piece) == 2
                            ? KingValue
                            : ManValue;

                    score += sign * pieceValue;

                    // Просування звичайних шашок
                    if (piece == Piece.White)
                    {
                        score +=
                            (Board.Size - 1 - r) *
                            AdvancementBonus;
                    }
                    else if (piece == Piece.Black)
                    {
                        score -=
                            r *
                            AdvancementBonus;
                    }

                    // Перевага центральних клітинок
                    if (r >= 2 &&
                        r <= 3 &&
                        c >= 2 &&
                        c <= 3)
                    {
                        score += sign * CenterBonus;
                    }
                }
            }

            return score;
        }


        public static int SimpleHeuristic(Board board)
        {
            int score = 0;

            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    Piece piece = board.Cells[r, c];

                    switch (piece)
                    {
                        case Piece.White:
                            score += ManValue;
                            break;

                        case Piece.WhiteKing:
                            score += KingValue;
                            break;

                        case Piece.Black:
                            score -= ManValue;
                            break;

                        case Piece.BlackKing:
                            score -= KingValue;
                            break;
                    }
                }
            }

            return score;
        }

       
        public static int Minimax(
            Board board,
            int depth,
            bool maximizingPlayer,
            Func<Board, int> heuristic)
        {
            PureMinimaxNodes++;

            List<Move> moves = board.GetLegalMoves();

            if (depth == 0 || moves.Count == 0)
            {
                if (moves.Count == 0)
                {
                    return UtilityFunction(board);
                }

                return heuristic(board);
            }

            if (maximizingPlayer)
            {
                int bestValue = -Infinity;

                foreach (Move move in moves)
                {
                    board.ApplyMove(move);

                    int value = Minimax(
                        board,
                        depth - 1,
                        false,
                        heuristic);

                    board.UndoMove(move);

                    bestValue = Math.Max(
                        bestValue,
                        value);
                }

                return bestValue;
            }
            else
            {
                int bestValue = Infinity;

                foreach (Move move in moves)
                {
                    board.ApplyMove(move);

                    int value = Minimax(
                        board,
                        depth - 1,
                        true,
                        heuristic);

                    board.UndoMove(move);

                    bestValue = Math.Min(
                        bestValue,
                        value);
                }

                return bestValue;
            }
        }

       
        private static int MoveOrderingScore(
            Board board,
            Move move)
        {
            int score = 0;

            if (move.IsCapture)
            {
                score += 10_000;
            }

            if (move.BecameKing)
            {
                score += 5_000;
            }

            Piece piece =
                board.Cells[
                    move.FromRow,
                    move.FromCol];

            if (piece == Piece.White)
            {
                score +=
                    (Board.Size - 1 - move.ToRow) *
                    100;
            }
            else if (piece == Piece.Black)
            {
                score +=
                    move.ToRow *
                    100;
            }

            if (move.ToRow >= 2 &&
                move.ToRow <= 3 &&
                move.ToCol >= 2 &&
                move.ToCol <= 3)
            {
                score += 50;
            }

            return score;
        }

      
        private static List<Move> OrderMoves(
            Board board,
            List<Move> moves,
            MoveOrder order)
        {
            List<Move> result =
                new List<Move>(moves);

            switch (order)
            {
                case MoveOrder.Natural:
                    return result;

                case MoveOrder.SortedCaptures:
                    return result
                        .OrderByDescending(
                            m => MoveOrderingScore(board, m))
                        .ToList();

                case MoveOrder.Worst:
                    return result
                        .OrderBy(
                            m => MoveOrderingScore(board, m))
                        .ToList();

                case MoveOrder.Random:
                    Random random = new Random(42);

                    for (int i = result.Count - 1;
                         i > 0;
                         i--)
                    {
                        int j =
                            random.Next(i + 1);

                        Move temp = result[i];
                        result[i] = result[j];
                        result[j] = temp;
                    }

                    return result;

                default:
                    return result;
            }
        }

      
        public static int AlphaBeta(
            Board board,
            int depth,
            int alpha,
            int beta,
            bool maximizingPlayer,
            Func<Board, int> heuristic,
            MoveOrder moveOrder,
            bool trace = false,
            int traceIndent = 0)
        {
            AlphaBetaNodes++;

            List<Move> moves = board.GetLegalMoves();

            if (depth == 0 || moves.Count == 0)
            {
                int value;

                if (moves.Count == 0)
                {
                    value = UtilityFunction(board);
                }
                else
                {
                    value = heuristic(board);
                }

                if (trace)
                {
                    Console.WriteLine(
                        $"{new string(' ', traceIndent)}HEF: val = {value}");
                }

                return value;
            }

            moves = OrderMoves(
                board,
                moves,
                moveOrder);

            if (trace)
            {
                Console.WriteLine(
                    $"{new string(' ', traceIndent)}" +
                    $"{(maximizingPlayer ? "MAX" : "MIN")} " +
                    $"[d={depth}, a={alpha}, b={beta}, " +
                    $"n_moves={moves.Count}]");
            }

            if (maximizingPlayer)
            {
                int value = -Infinity;

                foreach (Move move in moves)
                {
                    board.ApplyMove(move);

                    int childValue = AlphaBeta(
                        board,
                        depth - 1,
                        alpha,
                        beta,
                        false,
                        heuristic,
                        moveOrder,
                        trace,
                        traceIndent + 2);

                    board.UndoMove(move);

                    value = Math.Max(
                        value,
                        childValue);

                    alpha = Math.Max(
                        alpha,
                        value);

                    if (trace)
                    {
                        Console.WriteLine(
                            $"{new string(' ', traceIndent)}" +
                            $"MAX хід {move}: " +
                            $"val={childValue}, " +
                            $"alpha={alpha}, " +
                            $"beta={beta}");
                    }

                    if (beta <= alpha)
                    {
                        Cutoffs++;

                        if (trace)
                        {
                            Console.WriteLine(
                                $"{new string(' ', traceIndent)}" +
                                $">>> ВІДТИНАННЯ (MAX): " +
                                $"beta ({beta}) <= " +
                                $"alpha ({alpha})");
                        }

                        break;
                    }
                }

                return value;
            }
            else
            {
                int value = Infinity;

                foreach (Move move in moves)
                {
                    board.ApplyMove(move);

                    int childValue = AlphaBeta(
                        board,
                        depth - 1,
                        alpha,
                        beta,
                        true,
                        heuristic,
                        moveOrder,
                        trace,
                        traceIndent + 2);

                    board.UndoMove(move);

                    value = Math.Min(
                        value,
                        childValue);

                    beta = Math.Min(
                        beta,
                        value);

                    if (trace)
                    {
                        Console.WriteLine(
                            $"{new string(' ', traceIndent)}" +
                            $"MIN хід {move}: " +
                            $"val={childValue}, " +
                            $"alpha={alpha}, " +
                            $"beta={beta}");
                    }

                    if (beta <= alpha)
                    {
                        Cutoffs++;

                        if (trace)
                        {
                            Console.WriteLine(
                                $"{new string(' ', traceIndent)}" +
                                $">>> ВІДТИНАННЯ (MIN): " +
                                $"beta ({beta}) <= " +
                                $"alpha ({alpha})");
                        }

                        break;
                    }
                }

                return value;
            }
        }

      
        public static SearchResult RunAlphaBetaRoot(
            Board board,
            int depth,
            Func<Board, int> heuristic,
            MoveOrder moveOrder,
            bool trace = false)
        {
            AlphaBetaNodes = 0;
            Cutoffs = 0;

            bool maximizingPlayer = board.WhiteTurn;

            List<Move> moves =
                OrderMoves(
                    board,
                    board.GetLegalMoves(),
                    moveOrder);

            if (moves.Count == 0)
            {
                return new SearchResult
                {
                    Value = UtilityFunction(board),
                    BestMove = null,
                    Nodes = 1,
                    Cutoffs = 0
                };
            }

            int alpha = -Infinity;
            int beta = Infinity;

            int bestValue =
                maximizingPlayer
                    ? -Infinity
                    : Infinity;

            Move? bestMove = null;

            Console.WriteLine();

            if (trace)
            {
                Console.WriteLine(
                    "=== ТРАСУВАННЯ ПОШУКУ (ROOT) ===");

                Console.WriteLine(
                    $"{(maximizingPlayer ? "MAX" : "MIN")} root, " +
                    $"depth={depth}, " +
                    $"alpha={alpha}, beta={beta}");
            }

            foreach (Move move in moves)
            {
                board.ApplyMove(move);

                int value = AlphaBeta(
                    board,
                    depth - 1,
                    alpha,
                    beta,
                    !maximizingPlayer,
                    heuristic,
                    moveOrder,
                    trace,
                    2);

                board.UndoMove(move);

                if (maximizingPlayer)
                {
                    if (value > bestValue)
                    {
                        bestValue = value;
                        bestMove = move;
                    }

                    alpha = Math.Max(
                        alpha,
                        bestValue);
                }
                else
                {
                    if (value < bestValue)
                    {
                        bestValue = value;
                        bestMove = move;
                    }

                    beta = Math.Min(
                        beta,
                        bestValue);
                }

                if (trace)
                {
                    Console.WriteLine(
                        $"ROOT хід {move}: " +
                        $"val={value}, " +
                        $"alpha={alpha}, " +
                        $"beta={beta}");
                }

                if (beta <= alpha)
                {
                    Cutoffs++;

                    if (trace)
                    {
                        Console.WriteLine(
                            ">>> ВІДТИНАННЯ НА ROOT");
                    }

                    break;
                }
            }

            return new SearchResult
            {
                Value = bestValue,
                BestMove = bestMove,
                Nodes = AlphaBetaNodes + 1,
                Cutoffs = Cutoffs
            };
        }

      
        public static void CompareMinimaxAndAlphaBeta(
            Board original,
            int depth)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"--- Глибина {depth} ---");

            Board minimaxBoard =
                original.Clone();

            PureMinimaxNodes = 0;

            int mmValue = Minimax(
                minimaxBoard,
                depth,
                minimaxBoard.WhiteTurn,
                HeuristicEvaluation);

            long mmNodes =
                PureMinimaxNodes;

            Board alphaBetaBoard =
                original.Clone();

            SearchResult ab =
                RunAlphaBetaRoot(
                    alphaBetaBoard,
                    depth,
                    HeuristicEvaluation,
                    MoveOrder.SortedCaptures);

            double reduction =
                mmNodes == 0
                    ? 0
                    : 100.0 *
                      (mmNodes - ab.Nodes) /
                      mmNodes;

            bool sameValue =
                mmValue == ab.Value;

            Console.WriteLine(
                $"Minimax:       nodes = {mmNodes}, " +
                $"value = {mmValue}");

            Console.WriteLine(
                $"Alpha-Beta:    nodes = {ab.Nodes}, " +
                $"cutoffs = {ab.Cutoffs}, " +
                $"value = {ab.Value}");

            Console.WriteLine(
                $"Зменшення вузлів: {reduction:F2}%");

            Console.WriteLine(
                $"MM = AB:       {(sameValue ? "ТАК" : "НІ")}");

            Console.WriteLine(
                $"Найкращий хід: {ab.BestMove}");
        }

     
        public static void CompareMoveOrdering(
            Board original,
            int depth)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"--- Порядок ходів, depth={depth} ---");

            SearchResult good =
                RunAlphaBetaRoot(
                    original.Clone(),
                    depth,
                    HeuristicEvaluation,
                    MoveOrder.SortedCaptures);

            SearchResult worst =
                RunAlphaBetaRoot(
                    original.Clone(),
                    depth,
                    HeuristicEvaluation,
                    MoveOrder.Worst);

            Console.WriteLine(
                $"Good ordering:  nodes={good.Nodes}, " +
                $"cutoffs={good.Cutoffs}, " +
                $"value={good.Value}");

            Console.WriteLine(
                $"Worst ordering: nodes={worst.Nodes}, " +
                $"cutoffs={worst.Cutoffs}, " +
                $"value={worst.Value}");

            Console.WriteLine(
                $"Різниця вузлів: " +
                $"{worst.Nodes - good.Nodes}");
        }

      
        public static void CompareRandomOrdering(
            Board original,
            int depth)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"--- Sorted vs Random, depth={depth} ---");

            SearchResult sorted =
                RunAlphaBetaRoot(
                    original.Clone(),
                    depth,
                    HeuristicEvaluation,
                    MoveOrder.SortedCaptures);

            SearchResult random =
                RunAlphaBetaRoot(
                    original.Clone(),
                    depth,
                    HeuristicEvaluation,
                    MoveOrder.Random);

            Console.WriteLine(
                $"Sorted: nodes={sorted.Nodes}, " +
                $"cutoffs={sorted.Cutoffs}");

            Console.WriteLine(
                $"Random: nodes={random.Nodes}, " +
                $"cutoffs={random.Cutoffs}");
        }

      
        public static void CompareTacticalPosition()
        {
            Console.WriteLine();
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "ЕКСПЕРИМЕНТ: ТАКТИЧНА ПОЗИЦІЯ");

            Console.WriteLine(
                "============================================================");

            Board board =
                Board.InitTacticalPosition();

            board.Print();

            for (int depth = 2;
                 depth <= 8;
                 depth += 2)
            {
                SearchResult good =
                    RunAlphaBetaRoot(
                        board.Clone(),
                        depth,
                        HeuristicEvaluation,
                        MoveOrder.SortedCaptures);

                SearchResult worst =
                    RunAlphaBetaRoot(
                        board.Clone(),
                        depth,
                        HeuristicEvaluation,
                        MoveOrder.Worst);

                Console.WriteLine();

                Console.WriteLine(
                    $"Depth {depth}:");

                Console.WriteLine(
                    $"  Good : nodes={good.Nodes}, " +
                    $"cutoffs={good.Cutoffs}");

                Console.WriteLine(
                    $"  Worst: nodes={worst.Nodes}, " +
                    $"cutoffs={worst.Cutoffs}");
            }
        }

     
        public static void CompareHeuristics(
            Board original,
            int depth)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"--- Порівняння HEF, depth={depth} ---");

            SearchResult rich =
                RunAlphaBetaRoot(
                    original.Clone(),
                    depth,
                    HeuristicEvaluation,
                    MoveOrder.SortedCaptures);

            SearchResult simple =
                RunAlphaBetaRoot(
                    original.Clone(),
                    depth,
                    SimpleHeuristic,
                    MoveOrder.SortedCaptures);

            Console.WriteLine(
                $"Rich HEF:   nodes={rich.Nodes}, " +
                $"value={rich.Value}, " +
                $"cutoffs={rich.Cutoffs}");

            Console.WriteLine(
                $"Simple HEF: nodes={simple.Nodes}, " +
                $"value={simple.Value}, " +
                $"cutoffs={simple.Cutoffs}");
        }

     
        public static void DemonstrateTreeAndCutoff()
        {
            Console.WriteLine();
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "ДЕМОНСТРАЦІЯ ДЕРЕВА ПОШУКУ ТА ALPHA-BETA ВІДТИНАННЯ");

            Console.WriteLine(
                "============================================================");

            Board board =
                Board.InitTacticalPosition();

            board.Print();

            SearchResult result =
                RunAlphaBetaRoot(
                    board,
                    3,
                    HeuristicEvaluation,
                    MoveOrder.SortedCaptures,
                    true);

            Console.WriteLine();

            Console.WriteLine(
                $"Результат трасування: " +
                $"value={result.Value}, " +
                $"nodes={result.Nodes}, " +
                $"cutoffs={result.Cutoffs}");

            Console.WriteLine(
                $"Найкращий хід: {result.BestMove}");
        }

     
        public static void GenerateDotExample()
        {
            string fileName =
                "checkers_alpha_beta_tree.dot";

            string dot = """
digraph CheckersSearchTree {
    rankdir=TB;

    graph [
        fontname="Arial"
    ];

    node [
        shape=box,
        fontname="Arial",
        fontsize=10
    ];

    edge [
        fontname="Arial",
        fontsize=9
    ];

    Root [
        label="MAX ROOT\nd=3\nalpha=-∞, beta=+∞"
    ];

    Min1 [
        label="MIN d=2\nalpha=-∞, beta=+∞"
    ];

    Max1 [
        label="MAX d=1\nalpha=-∞, beta=+∞"
    ];

    Leaf1 [
        label="HEF = -350"
    ];

    Leaf2 [
        label="HEF = -350"
    ];

    Leaf3 [
        label="HEF = -335"
    ];

    Leaf4 [
        label="HEF = -335"
    ];

    Min2 [
        label="MIN d=2\nalpha=-∞, beta=-335"
    ];

    Max2 [
        label="MAX d=1\nalpha=-∞, beta=-335"
    ];

    Leaf5 [
        label="HEF = -115"
    ];

    Cutoff [
        label="ВІДТИНАННЯ\nbeta=-335 <= alpha=-115",
        style=dashed
    ];

    Root -> Min1 [
        label="[4,1] x [2,3]"
    ];

    Min1 -> Max1 [
        label="[3,4] x [5,2]"
    ];

    Max1 -> Leaf1 [
        label="[2,3] -> [1,2]"
    ];

    Max1 -> Leaf2 [
        label="[2,3] -> [1,4]"
    ];

    Max1 -> Leaf3 [
        label="[5,4] -> [4,3]"
    ];

    Max1 -> Leaf4 [
        label="[5,4] -> [4,5]"
    ];

    Min1 -> Min2 [
        label="[3,4] x [1,2]"
    ];

    Min2 -> Max2 [
        label="наступна гілка"
    ];

    Max2 -> Leaf5 [
        label="[4,3] -> [3,2]"
    ];

    Max2 -> Cutoff [
        label="інші ходи\nне досліджуються",
        style=dotted
    ];
}
""";

        
            File.WriteAllText(
                fileName,
                dot,
                new UTF8Encoding(false));

            Console.WriteLine();
            Console.WriteLine(
                $"Graphviz-файл створено: {fileName}");
        }
    }

   
    public class Program
    {
        public static void Main()
        {
            Console.OutputEncoding =
                Encoding.UTF8;

            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "       Лабораторна робота №2: Ігровий пошук Minimax");

            Console.WriteLine(
                "============================================================");

            Console.WriteLine();

            Console.WriteLine(
                "Правила гри:");

            Console.WriteLine(
                "1. Дошка має розмір 6x6.");

            Console.WriteLine(
                "2. Гравці по черзі виконують ходи.");

            Console.WriteLine(
                "3. Білий гравець максимізує оцінку.");

            Console.WriteLine(
                "4. Чорний гравець мінімізує оцінку.");

            Console.WriteLine(
                "5. Якщо є взяття, воно є обов'язковим.");

            Console.WriteLine(
                "6. Взяття виконується одним стрибком.");

            Console.WriteLine(
                "7. Багатоходові взяття не використовуються.");

            Console.WriteLine(
                "8. Звичайна шашка рухається вперед.");

            Console.WriteLine(
                "9. Король рухається по діагоналі в усіх напрямках.");

            Console.WriteLine(
                "10. Перехід на останню горизонталь перетворює шашку на короля.");

           
            Board board =
                Board.InitDefault();

            Console.WriteLine();
            Console.WriteLine(
                "Початкова позиція:");

            board.Print();

            
            Console.WriteLine();
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "ЕКСПЕРИМЕНТ 1: MINIMAX VS ALPHA-BETA");

            Console.WriteLine(
                "============================================================");

            CompareDepths(board);

           
            Console.WriteLine();
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "ЕКСПЕРИМЕНТ 2: ВПЛИВ ПОРЯДКУ ХОДІВ");

            Console.WriteLine(
                "============================================================");

            foreach (int depth in new[] { 2, 4, 6 })
            {
                SearchAlgorithms.CompareMoveOrdering(
                    board,
                    depth);
            }

           
            Console.WriteLine();
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "ЕКСПЕРИМЕНТ 3: SORTED VS RANDOM");

            Console.WriteLine(
                "============================================================");

            foreach (int depth in new[] { 2, 4, 6 })
            {
                SearchAlgorithms.CompareRandomOrdering(
                    board,
                    depth);
            }

            SearchAlgorithms.CompareTacticalPosition();

            Console.WriteLine();
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "ЕКСПЕРИМЕНТ 5: ПОРІВНЯННЯ ЕВРИСТИК");

            Console.WriteLine(
                "============================================================");

            foreach (int depth in new[] { 2, 4, 6 })
            {
                SearchAlgorithms.CompareHeuristics(
                    board,
                    depth);
            }

            SearchAlgorithms.DemonstrateTreeAndCutoff();

            SearchAlgorithms.GenerateDotExample();

            Console.WriteLine();
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "РОБОТУ ЗАВЕРШЕНО");

            Console.WriteLine(
                "============================================================");
        }

        private static void CompareDepths(Board board)
        {
            foreach (int depth in new[] { 2, 4, 6 })
            {
                SearchAlgorithms.CompareMinimaxAndAlphaBeta(
                    board,
                    depth);
            }
        }
    }
}